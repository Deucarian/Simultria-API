#!/usr/bin/env python3
"""Generate deterministic Simultria catalog data from a local OpenAPI snapshot.

This package-author tool never performs network requests. YAML input requires
PyYAML; JSON input uses only the Python standard library.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
from typing import Any, Dict, Iterable, List, Mapping, Tuple


HTTP_METHODS = ("get", "post", "put", "patch", "delete")
AUTHENTICATION_VALUES = {
    "UseConfigDefault",
    "Required",
    "Optional",
    "Disabled",
}
RESPONSE_FORMAT_VALUES = {"Auto", "Json", "Text", "Bytes"}


class ContractError(Exception):
    """A safe, actionable package-author input error."""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Generate catalog and coverage JSON from local OpenAPI and "
            "Deucarian overlay files. No network access is performed."
        )
    )
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--overlay", required=True, type=Path)
    parser.add_argument("--catalog-out", required=True, type=Path)
    parser.add_argument("--coverage-out", required=True, type=Path)
    parser.add_argument(
        "--unity-asset-out",
        type=Path,
        help=(
            "Optional Unity ApiEndpointCatalog asset output. Use --check to "
            "verify the checked-in runtime asset."
        ),
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail when either existing output differs; do not write files.",
    )
    parser.add_argument(
        "--require-complete",
        action="store_true",
        help=(
            "Fail unless every supported-method operation is emitted and "
            "every overlay key matches. This only proves coverage of the "
            "supplied snapshot."
        ),
    )
    return parser.parse_args()


def reject_non_local(path: Path, label: str) -> None:
    value = str(path)
    if "://" in value:
        raise ContractError(
            f"{label} must be a local file; network URLs are not accepted."
        )
    if not path.is_file():
        raise ContractError(f"{label} file does not exist: {path}")


def load_document(path: Path, label: str) -> Mapping[str, Any]:
    reject_non_local(path, label)
    text = path.read_text(encoding="utf-8-sig")
    try:
        value = json.loads(text)
    except json.JSONDecodeError as json_error:
        try:
            import yaml  # type: ignore
        except ImportError as import_error:
            raise ContractError(
                f"{label} is not JSON. Install PyYAML to read YAML files."
            ) from import_error
        try:
            value = yaml.safe_load(text)
        except Exception as yaml_error:
            raise ContractError(
                f"{label} is neither valid JSON nor valid YAML: {json_error}"
            ) from yaml_error
    if not isinstance(value, Mapping):
        raise ContractError(f"{label} root must be an object/map.")
    return value


def require_text(
    source: Mapping[str, Any], key: str, label: str
) -> str:
    value = source.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ContractError(f"{label} requires non-empty '{key}'.")
    return value.strip()


def normalized_route(path: str) -> str:
    route = path.strip().lstrip("/")
    if not route or "://" in route:
        raise ContractError(
            f"OpenAPI path must resolve to a relative route: {path!r}"
        )
    return route


def derived_endpoint_id(method: str, path: str) -> str:
    route = normalized_route(path).lower()
    route = re.sub(r"\{([^}]+)\}", r"by-\1", route)
    route = re.sub(r"[^a-z0-9._-]+", ".", route)
    route = re.sub(r"[._-]{2,}", ".", route).strip("._-")
    prefix = f"simultria.generated.{method.lower()}."
    candidate = prefix + route
    if len(candidate) <= 128:
        return candidate
    digest = hashlib.sha256(f"{method} {path}".encode("utf-8")).hexdigest()[:12]
    retained = 128 - len(prefix) - len(digest) - 1
    return prefix + route[:retained].rstrip("._-") + "." + digest


def resolved_authentication(
    spec: Mapping[str, Any], operation: Mapping[str, Any]
) -> str:
    security = operation.get("security", spec.get("security"))
    return "Disabled" if security == [] else "Required"


def operation_rows(
    spec: Mapping[str, Any]
) -> Iterable[Tuple[str, str, Mapping[str, Any]]]:
    paths = spec.get("paths")
    if not isinstance(paths, Mapping):
        raise ContractError("OpenAPI snapshot requires a 'paths' object.")
    for path in sorted(paths):
        path_item = paths[path]
        if not isinstance(path, str) or not isinstance(path_item, Mapping):
            raise ContractError("Every OpenAPI path item must be an object.")
        unsupported_methods = [
            method.upper()
            for method in ("head", "options", "trace")
            if method in path_item
        ]
        if unsupported_methods:
            raise ContractError(
                "OpenAPI operation(s) cannot be represented by Deucarian "
                f"HttpMethod at {path}: {', '.join(unsupported_methods)}"
            )
        for method in HTTP_METHODS:
            operation = path_item.get(method)
            if operation is None:
                continue
            if not isinstance(operation, Mapping):
                raise ContractError(
                    f"OpenAPI operation {method.upper()} {path} must be an object."
                )
            yield method.upper(), path, operation


def overlay_operations(
    overlay: Mapping[str, Any]
) -> Mapping[str, Mapping[str, Any]]:
    operations = overlay.get("operations")
    if not isinstance(operations, Mapping):
        raise ContractError("Overlay requires an 'operations' object.")
    result: Dict[str, Mapping[str, Any]] = {}
    for key, value in operations.items():
        if not isinstance(key, str) or not isinstance(value, Mapping):
            raise ContractError(
                "Every overlay operation key must map to an object."
            )
        result[key.strip()] = value
    return result


def find_overlay_entry(
    method: str,
    path: str,
    operation: Mapping[str, Any],
    operations: Mapping[str, Mapping[str, Any]],
) -> Tuple[str, Mapping[str, Any]] | Tuple[None, None]:
    operation_id = operation.get("operationId")
    candidates: List[str] = []
    if isinstance(operation_id, str) and operation_id.strip():
        candidates.append(operation_id.strip())
    candidates.append(f"{method} {path}")
    for key in candidates:
        if key in operations:
            return key, operations[key]
    return None, None


def normalize_policy(value: Any, label: str) -> Dict[str, Any]:
    if value is None:
        return {}
    if not isinstance(value, Mapping):
        raise ContractError(f"{label} requestPolicy must be an object.")
    allowed = {
        "timeoutSeconds",
        "maxRetryAttempts",
        "initialRetryBackoffMilliseconds",
        "retryBackoffMultiplier",
        "maximumRetryBackoffMilliseconds",
        "rateLimitRequestCountHint",
        "rateLimitWindowSecondsHint",
    }
    unknown = sorted(set(value.keys()) - allowed)
    if unknown:
        raise ContractError(
            f"{label} requestPolicy has unknown keys: {', '.join(unknown)}"
        )
    return {key: value[key] for key in sorted(value)}


def normalize_pairs(value: Any, label: str) -> List[Dict[str, str]]:
    if value is None:
        return []
    if not isinstance(value, list):
        raise ContractError(f"{label} must be an array.")
    result: List[Dict[str, str]] = []
    for index, item in enumerate(value):
        if not isinstance(item, Mapping):
            raise ContractError(f"{label}[{index}] must be an object.")
        key = require_text(item, "key", f"{label}[{index}]")
        raw_value = item.get("value", "")
        if not isinstance(raw_value, str):
            raise ContractError(f"{label}[{index}].value must be text.")
        result.append({"key": key, "value": raw_value})
    return sorted(result, key=lambda pair: (pair["key"], pair["value"]))


def build_endpoint(
    method: str,
    path: str,
    entry: Mapping[str, Any],
    default_client_id: str,
    label: str,
    fallback_endpoint_id: str,
    default_authentication: str,
    default_suppress_logging: bool,
) -> Dict[str, Any]:
    authentication = entry.get(
        "authentication",
        default_authentication,
    )
    if authentication not in AUTHENTICATION_VALUES:
        raise ContractError(
            f"{label} has unsupported authentication: {authentication!r}"
        )
    response_format = entry.get("responseFormat", "Auto")
    if response_format not in RESPONSE_FORMAT_VALUES:
        raise ContractError(
            f"{label} has unsupported responseFormat: {response_format!r}"
        )
    suppress_logging = entry.get(
        "suppressLogging",
        default_suppress_logging,
    )
    if not isinstance(suppress_logging, bool):
        raise ContractError(f"{label} suppressLogging must be true or false.")
    return {
        "endpointId": str(
            entry.get("endpointId", fallback_endpoint_id)
        ).strip(),
        "clientId": str(entry.get("clientId", default_client_id)).strip(),
        "routeTemplate": normalized_route(path),
        "method": method,
        "authentication": authentication,
        "responseFormat": response_format,
        "defaultHeaders": normalize_pairs(
            entry.get("defaultHeaders"), f"{label}.defaultHeaders"
        ),
        "defaultQueryParameters": normalize_pairs(
            entry.get("defaultQueryParameters"),
            f"{label}.defaultQueryParameters",
        ),
        "requestPolicy": normalize_policy(entry.get("requestPolicy"), label),
        "suppressLogging": suppress_logging,
    }


def generate(
    spec: Mapping[str, Any],
    overlay: Mapping[str, Any],
    spec_path: Path,
) -> Tuple[Dict[str, Any], Dict[str, Any]]:
    openapi_version = require_text(spec, "openapi", "OpenAPI snapshot")
    catalog_id = require_text(overlay, "catalogId", "Overlay")
    display_name = require_text(overlay, "displayName", "Overlay")
    default_client_id = require_text(
        overlay, "defaultClientId", "Overlay"
    )
    operations = overlay_operations(overlay)
    used_overlay_keys = set()
    endpoints: List[Dict[str, Any]] = []
    overlay_mapped: List[Dict[str, str]] = []
    derived: List[Dict[str, str]] = []

    rows = list(operation_rows(spec))
    for method, path, operation in rows:
        overlay_key, entry = find_overlay_entry(
            method, path, operation, operations
        )
        if entry is None:
            entry = {}
            derived.append(
                {
                    "method": method,
                    "path": path,
                    "operationId": str(operation.get("operationId", "")),
                }
            )
        else:
            used_overlay_keys.add(overlay_key)
            overlay_mapped.append(
                {
                    "method": method,
                    "path": path,
                    "operationId": str(operation.get("operationId", "")),
                    "overlayKey": overlay_key,
                }
            )
        endpoints.append(
            build_endpoint(
                method,
                path,
                entry,
                default_client_id,
                f"operation {method} {path}",
                derived_endpoint_id(method, path),
                resolved_authentication(spec, operation),
                overlay_key is None,
            )
        )

    endpoint_ids = [endpoint["endpointId"] for endpoint in endpoints]
    duplicates = sorted(
        endpoint_id
        for endpoint_id in set(endpoint_ids)
        if endpoint_ids.count(endpoint_id) > 1
    )
    if duplicates:
        raise ContractError(
            "Overlay produces duplicate endpoint IDs: " + ", ".join(duplicates)
        )

    unused_overlay = sorted(set(operations) - used_overlay_keys)
    source_hash = hashlib.sha256(spec_path.read_bytes()).hexdigest()
    catalog = {
        "schemaVersion": 1,
        "catalogId": catalog_id,
        "displayName": display_name,
        "source": {
            "fileName": spec_path.name,
            "openapiVersion": openapi_version,
            "sha256": source_hash,
        },
        "endpoints": sorted(
            endpoints,
            key=lambda endpoint: (
                endpoint["endpointId"],
                endpoint["method"],
                endpoint["routeTemplate"],
            ),
        ),
    }
    coverage = {
        "schemaVersion": 1,
        "sourceFileName": spec_path.name,
        "sourceSha256": source_hash,
        "operationsInSuppliedSnapshot": len(rows),
        "catalogOperations": len(endpoints),
        "overlayMappedOperations": len(overlay_mapped),
        "deterministicallyDerivedOperations": len(derived),
        "derivedLoggingSuppressedByDefault": True,
        "operationsNotInCatalog": [],
        "overlayMappings": sorted(
            overlay_mapped,
            key=lambda item: (item["path"], item["method"]),
        ),
        "derivedMappings": sorted(
            derived,
            key=lambda item: (item["path"], item["method"]),
        ),
        "unusedOverlayKeys": unused_overlay,
        "scopeNote": (
            "Coverage applies only to the supplied local snapshot and does not "
            "claim completeness against any live Simultria deployment."
        ),
        "snapshotCoverage": f"{len(endpoints)}/{len(rows)}",
        "snapshotCoverageComplete": len(endpoints) == len(rows),
    }
    return catalog, coverage


def unity_asset(value: Mapping[str, Any]) -> str:
    method_values = {
        "GET": 0,
        "POST": 1,
        "PUT": 2,
        "DELETE": 3,
        "PATCH": 4,
    }
    authentication_values = {
        "UseConfigDefault": 0,
        "Required": 1,
        "Optional": 2,
        "Disabled": 3,
    }
    response_values = {
        "Auto": 0,
        "Json": 1,
        "Text": 2,
        "Bytes": 3,
    }
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        (
            "  m_Script: {fileID: 11500000, "
            "guid: eff91661a22149c8aebd90e85283eb76, type: 3}"
        ),
        "  m_Name: SimultriaApiV2EndpointCatalog",
        (
            "  m_EditorClassIdentifier: Deucarian.API::"
            "Deucarian.API.Configuration.ApiEndpointCatalog"
        ),
        "  catalogId: " + str(value["catalogId"]),
        "  displayName: " + str(value["displayName"]),
        "  endpoints:",
    ]
    defaults = {
        "timeoutSeconds": -1,
        "maxRetryAttempts": -1,
        "initialRetryBackoffMilliseconds": -1,
        "retryBackoffMultiplier": 0,
        "maximumRetryBackoffMilliseconds": -1,
        "rateLimitRequestCountHint": -1,
        "rateLimitWindowSecondsHint": -1,
    }
    for endpoint in value["endpoints"]:
        policy = dict(defaults)
        policy.update(endpoint["requestPolicy"])
        lines.extend(
            [
                "  - endpointId: " + endpoint["endpointId"],
                "    clientId: " + endpoint["clientId"],
                "    routeTemplate: " + endpoint["routeTemplate"],
                "    method: " + str(method_values[endpoint["method"]]),
                (
                    "    authentication: "
                    + str(authentication_values[endpoint["authentication"]])
                ),
                "    responseFormat: "
                + str(response_values[endpoint["responseFormat"]]),
            ]
        )
        append_unity_pairs(
            lines,
            "defaultHeaders",
            endpoint["defaultHeaders"],
        )
        append_unity_pairs(
            lines,
            "defaultQueryParameters",
            endpoint["defaultQueryParameters"],
        )
        lines.extend(
            [
                "    requestPolicy:",
                "      timeoutSeconds: " + str(policy["timeoutSeconds"]),
                "      maxRetryAttempts: "
                + str(policy["maxRetryAttempts"]),
                "      initialRetryBackoffMilliseconds: "
                + str(policy["initialRetryBackoffMilliseconds"]),
                "      retryBackoffMultiplier: "
                + str(policy["retryBackoffMultiplier"]),
                "      maximumRetryBackoffMilliseconds: "
                + str(policy["maximumRetryBackoffMilliseconds"]),
                "      rateLimitRequestCountHint: "
                + str(policy["rateLimitRequestCountHint"]),
                "      rateLimitWindowSecondsHint: "
                + str(policy["rateLimitWindowSecondsHint"]),
                "    suppressLogging: "
                + ("1" if endpoint["suppressLogging"] else "0"),
            ]
        )
    return "\n".join(lines) + "\n"


def append_unity_pairs(
    lines: List[str],
    property_name: str,
    pairs: Iterable[Mapping[str, str]],
) -> None:
    pairs = list(pairs)
    if not pairs:
        lines.append(f"    {property_name}: []")
        return
    lines.append(f"    {property_name}:")
    for pair in pairs:
        lines.append("    - key: " + json.dumps(pair["key"]))
        lines.append("      value: " + json.dumps(pair["value"]))


def encoded(value: Mapping[str, Any]) -> str:
    return json.dumps(
        value,
        indent=2,
        sort_keys=True,
        ensure_ascii=False,
    ) + "\n"


def check_output(path: Path, expected: str, label: str) -> None:
    if not path.is_file():
        raise ContractError(f"{label} output is missing: {path}")
    actual = path.read_text(encoding="utf-8")
    if actual != expected:
        raise ContractError(
            f"{label} output is stale. Regenerate it from the approved snapshot."
        )


def write_output(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    args = parse_args()
    try:
        spec = load_document(args.spec, "OpenAPI snapshot")
        overlay = load_document(args.overlay, "Overlay")
        catalog, coverage = generate(spec, overlay, args.spec)
        if args.require_complete and (
            coverage["catalogOperations"] !=
            coverage["operationsInSuppliedSnapshot"] or
            coverage["operationsNotInCatalog"] or
            coverage["unusedOverlayKeys"]
        ):
            raise ContractError(
                "Not every supported-method operation was emitted or an "
                "overlay key did not match the supplied snapshot."
            )

        catalog_text = encoded(catalog)
        coverage_text = encoded(coverage)
        unity_asset_text = unity_asset(catalog)
        if args.check:
            check_output(args.catalog_out, catalog_text, "Catalog")
            check_output(args.coverage_out, coverage_text, "Coverage")
            if args.unity_asset_out is not None:
                check_output(
                    args.unity_asset_out,
                    unity_asset_text,
                    "Unity catalog asset",
                )
        else:
            write_output(args.catalog_out, catalog_text)
            write_output(args.coverage_out, coverage_text)
            if args.unity_asset_out is not None:
                write_output(args.unity_asset_out, unity_asset_text)
        return 0
    except ContractError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
