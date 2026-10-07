"""Generate the browsable C# client from a sanitized contract, without network access."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import uuid

import generate_contract as contract
from client_schema import clean_schema, identifier
from client_render import render

CONTRACT = Path("Documentation~/Generated/SimultriaApiClient.contract.json")
GENERATED = Path("Runtime/Generated")
CATALOG = Path("Documentation~/Generated/SimultriaApiV2EndpointCatalog.generated.json")
FRIENDLY = {
    "simultria.auth.login": ("Authentication", "Login"),
    "simultria.auth.validate": ("Authentication", "Validate"),
    "simultria.projects.list": ("Projects", "GetProjects"),
    "simultria.projects.get": ("Projects", "GetProject"),
    "simultria.project-models.list": ("Projects", "GetModels"),
    "simultria.models.get": ("Models", "GetModel"),
    "simultria.model-versions.get": ("ModelVersions", "GetModelVersion"),
    "simultria.model-versions.active": ("Models", "GetActiveVersion"),
    "simultria.model-versions.frozen": ("Models", "GetFrozenVersion"),
    "simultria.model-versions.download": ("ModelVersions", "Download"),
    "simultria.activities.list": ("ModelVersions", "GetActivities"),
    "simultria.activities.get": ("ModelVersions", "GetActivity"),
    "simultria.unity-builds.version": ("UnityBuildVersions", "GetVersion"),
}
GROUPS = {"Project Models": "Models", "Project Model Versions": "ModelVersions"}


def resolve(value, spec):
    if "$ref" not in value:
        return value
    ref = value["$ref"]
    if not ref.startswith("#/"):
        raise contract.ContractError("Only local OpenAPI references are supported.")
    try:
        for part in ref[2:].split("/"):
            spec = spec[part.replace("~1", "/").replace("~0", "~")]
    except (KeyError, TypeError) as error:
        raise contract.ContractError("Unresolved reference: " + ref) from error
    return spec


def response_contract(operation, spec):
    schemas = []
    formats = []
    for status, raw in sorted(operation.get("responses", {}).items()):
        if not str(status).startswith("2"):
            continue
        response = resolve(raw, spec)
        content = response.get("content", {})
        if not content:
            continue
        mime = "application/json" if "application/json" in content else sorted(content)[0]
        schema = clean_schema(content[mime].get("schema", {}), spec)
        fmt = "Json" if "json" in mime else "Text" if mime.startswith("text/") else "Bytes"
        schemas.append(schema)
        formats.append(fmt)
    if not schemas:
        return {}, "Auto"
    if len(set(formats)) != 1:
        raise contract.ContractError("Success responses have incompatible media types.")
    if formats[0] == "Bytes":
        return {"type": "string", "format": "binary"}, "Bytes"
    if formats[0] == "Text":
        return {"type": "string"}, "Text"
    if any(schema != schemas[0] for schema in schemas):
        return {}, "Json"
    return schemas[0], "Json"


def build(spec, catalog):
    source_hash = contract.contract_source_hash(spec)
    if source_hash != catalog["source"]["sha256"]:
        raise contract.ContractError("Client source does not match the catalog snapshot.")
    routes = {(e["method"], e["routeTemplate"]): e for e in catalog["endpoints"]}
    operations = []
    used = set()
    for method, path, operation in contract.operation_rows(spec):
        entry = routes[(method, contract.normalized_route(path))]
        endpoint_id = entry["endpointId"]
        tag = (operation.get("tags") or ["General"])[0]
        group, name = FRIENDLY.get(endpoint_id, (
            GROUPS.get(tag, identifier(tag)), identifier(operation.get("operationId") or method + path)))
        if (group, name) in used:
            name += hashlib.sha256(endpoint_id.encode()).hexdigest()[:8]
        used.add((group, name))
        params = {}
        for raw in spec["paths"][path].get("parameters", []) + operation.get("parameters", []):
            param = resolve(raw, spec)
            params[(param["in"], param["name"])] = param
        placeholders = re.findall(r"\{([^}]+)\}", path)
        for placeholder in placeholders:
            if ("path", placeholder) not in params:
                raise contract.ContractError("Missing schema for path parameter: " + path + " " + placeholder)
        selected = []
        for (location, wire), param in sorted(params.items()):
            # Scribe includes host/version route context which is already resolved by composition.
            if location == "path" and wire not in placeholders:
                continue
            if location not in ("path", "query", "header"):
                raise contract.ContractError("Unsupported parameter location: " + location)
            schema = clean_schema(param.get("schema", {}), spec)
            if schema.get("type") not in ("string", "integer", "number", "boolean"):
                raise contract.ContractError("Unsupported parameter serialization: " + path + " " + wire)
            if param.get("style", "simple" if location != "query" else "form") not in ("simple", "form"):
                raise contract.ContractError("Unsupported parameter style: " + wire)
            selected.append({"name": wire, "in": location, "required": location == "path" or param.get("required", False),
                             "schema": schema})
        body = resolve(operation.get("requestBody", {}), spec)
        content = body.get("content", {})
        mime = "application/json" if "application/json" in content else (sorted(content)[0] if content else "")
        if mime not in ("", "application/json", "multipart/form-data"):
            raise contract.ContractError("Unsupported request media type: " + mime)
        response, response_format = response_contract(operation, spec)
        operations.append({"id": endpoint_id, "group": group, "name": name, "method": method,
                           "route": entry["routeTemplate"], "parameters": selected,
                           "body": clean_schema(content[mime].get("schema", {}), spec) if mime else None,
                           "bodyRequired": body.get("required", False), "bodyFormat": mime,
                           "response": response, "responseFormat": response_format})
    return {"schemaVersion": 1, "sourceSha256": source_hash, "operations": sorted(operations, key=lambda o: o["id"])}


def meta(relative, folder=False):
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "simultria.backend-api/typed-client/" + relative).hex
    if folder:
        return f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
    return f"fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"


def sync(root, description, check=False):
    root = Path(root).resolve()
    files = render(description)
    expected = {GENERATED / name: value for name, value in files.items()}
    expected[CONTRACT] = contract.encoded(description)
    folders = {GENERATED}
    for path in list(expected):
        if path.suffix != ".cs":
            continue
        expected[Path(str(path) + ".meta")] = meta(path.as_posix())
        folders.update(p for p in path.parents if p != Path(".") and p != Path("Runtime"))
    for folder in folders:
        expected[Path(str(folder) + ".meta")] = meta(folder.as_posix(), True)
    for path, value in expected.items():
        destination = root / path
        if check:
            contract.check_output(destination, value, "Typed client " + str(path))
        else:
            contract.write_output(destination, value)
    generated_root = (root / GENERATED).resolve()
    for path in generated_root.rglob("*.g.cs"):
        if path.relative_to(root) in expected:
            continue
        if check:
            raise contract.ContractError("Stale generated client file: " + str(path))
        if not path.resolve().is_relative_to(generated_root) or not path.read_text().startswith("// <auto-generated />"):
            raise contract.ContractError("Refusing to remove a non-generated source: " + str(path))
        path.unlink()
        companion = Path(str(path) + ".meta")
        if companion.exists():
            companion.unlink()


def validate(root, catalog):
    description = contract.load_document(Path(root) / CONTRACT, "Typed client contract")
    if description["sourceSha256"] != catalog["source"]["sha256"]:
        raise contract.ContractError("Typed client and catalog fingerprints differ.")
    generated = {o["id"]: (o["method"], o["route"]) for o in description["operations"]}
    expected = {o["endpointId"]: (o["method"], o["routeTemplate"]) for o in catalog["endpoints"]}
    if generated != expected or len(generated) != len(description["operations"]):
        raise contract.ContractError("Typed client does not cover exactly the catalog operations.")
    sync(root, description, check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--output-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    try:
        catalog = contract.load_document(args.output_root / CATALOG, "Catalog")
        description = build(contract.load_document(args.spec, "OpenAPI"), catalog)
        sync(args.output_root, description, args.check)
        print("Generated typed operations:", len(description["operations"]))
    except contract.ContractError as error:
        parser.exit(2, str(error) + "\n")
