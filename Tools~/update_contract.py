#!/usr/bin/env python3
"""One-command Simultria OpenAPI import for authors and CI.

The command only accepts local files. It can update the package checkout,
generate into a preview directory, or verify that checked-in generated files
remain internally consistent without possessing the source OpenAPI snapshot.
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import subprocess
import sys
from typing import Any, Mapping

import generate_contract


PACKAGE_ROOT = Path(__file__).resolve().parent.parent
OVERLAY = Path("Documentation~/simultria-api-v2.supported-subset.overlay.json")
CATALOG = Path(
    "Documentation~/Generated/SimultriaApiV2EndpointCatalog.generated.json"
)
COVERAGE = Path(
    "Documentation~/Generated/SimultriaApiV2EndpointCatalog.coverage.json"
)
MANIFEST = Path(
    "Documentation~/Generated/SimultriaApiV2Contract.manifest.json"
)
DOCUMENTATION = Path("Documentation~/Generated/API-Endpoints.md")
UNITY_ASSET = Path(
    "Runtime/Resources/Deucarian/Simultria/API/"
    "SimultriaApiV2EndpointCatalog.asset"
)
SERVICE_DEFINITION_ASSET = Path(
    "Runtime/Resources/Deucarian/Simultria/API/"
    "SimultriaApiV2Definition.asset"
)


def service_definition_asset(manifest: Mapping[str, Any]) -> str:
    source = manifest.get("source")
    if not isinstance(source, Mapping):
        raise generate_contract.ContractError(
            "Generated manifest requires source metadata."
        )
    revision = require_source_revision(str(source.get("backendRevision", "")))
    fingerprint = str(source.get("sha256", "")).strip().lower()
    if len(fingerprint) != 64:
        raise generate_contract.ContractError(
            "Generated manifest requires a SHA-256 source fingerprint."
        )
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 863d2f1eaec64cac8e6cd2abaa856dfe, type: 3}}
  m_Name: SimultriaApiV2Definition
  m_EditorClassIdentifier: Deucarian.API::Deucarian.API.Configuration.ApiServiceDefinition
  serviceId: simultria.api-v2
  displayName: Simultria API v2
  endpointCatalog: {{fileID: 11400000, guid: 5f65e932f762430bbb9132a72ba857d4, type: 2}}
  knownEnvironments:
  - environmentId: simultria.local
    stage: 0
    displayName: Local
  - environmentId: simultria.development
    stage: 1
    displayName: Development
  - environmentId: simultria.testing
    stage: 2
    displayName: Testing
  - environmentId: simultria.acceptance
    stage: 3
    displayName: Acceptance
  - environmentId: simultria.production
    stage: 4
    displayName: Production
  requiredClients:
  - clientId: simultria.primary
    displayName: Simultria API
  sourceVersion: {revision}
  sourceFingerprint: sha256:{fingerprint}
"""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Update or validate every generated Simultria API contract "
            "artifact from one local OpenAPI snapshot."
        )
    )
    parser.add_argument("--spec", type=Path)
    parser.add_argument(
        "--source-revision",
        default=os.environ.get("SIMULTRIA_BACKEND_COMMIT", ""),
        help=(
            "Backend Git commit that generated the spec. Defaults to "
            "SIMULTRIA_BACKEND_COMMIT."
        ),
    )
    parser.add_argument(
        "--output-root",
        type=Path,
        default=PACKAGE_ROOT,
        help="Package-shaped output root. Defaults to this package checkout.",
    )
    parser.add_argument(
        "--baseline-catalog",
        type=Path,
        default=PACKAGE_ROOT / CATALOG,
        help="Catalog to compare for the semantic change report.",
    )
    parser.add_argument("--change-report-out", type=Path)
    parser.add_argument("--change-report-markdown-out", type=Path)
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify outputs without rewriting package artifacts.",
    )
    parser.add_argument(
        "--validate-generated",
        action="store_true",
        help=(
            "Validate checked-in artifacts against each other without the "
            "source OpenAPI file."
        ),
    )
    parser.add_argument(
        "--refresh-review-files",
        action="store_true",
        help=(
            "Regenerate only the manifest and Markdown reference from the "
            "checked-in catalog and coverage."
        ),
    )
    return parser.parse_args()


def require_source_revision(value: str) -> str:
    revision = generate_contract.normalized_source_revision(value)
    if not revision:
        raise generate_contract.ContractError(
            "--source-revision or SIMULTRIA_BACKEND_COMMIT is required."
        )
    return revision


def read_json(path: Path, label: str) -> Mapping[str, Any]:
    return generate_contract.load_document(path, label)


def output_path(root: Path, relative: Path) -> Path:
    return root.resolve() / relative


def check_text(path: Path, expected: str, label: str) -> None:
    generate_contract.check_output(path, expected, label)


def validate_generated(root: Path) -> None:
    catalog_path = output_path(root, CATALOG)
    coverage_path = output_path(root, COVERAGE)
    manifest_path = output_path(root, MANIFEST)
    documentation_path = output_path(root, DOCUMENTATION)
    unity_asset_path = output_path(root, UNITY_ASSET)
    service_definition_path = output_path(root, SERVICE_DEFINITION_ASSET)

    catalog = read_json(catalog_path, "Generated catalog")
    coverage = read_json(coverage_path, "Generated coverage")
    manifest = read_json(manifest_path, "Generated manifest")
    source = manifest.get("source")
    if not isinstance(source, Mapping):
        raise generate_contract.ContractError(
            "Generated manifest requires source metadata."
        )
    revision = require_source_revision(str(source.get("backendRevision", "")))

    expected_manifest = generate_contract.contract_manifest(
        catalog,
        coverage,
        revision,
    )
    expected_documentation = generate_contract.contract_documentation(
        catalog,
        coverage,
        expected_manifest,
    )
    check_text(
        manifest_path,
        generate_contract.encoded(expected_manifest),
        "Contract manifest",
    )
    check_text(
        documentation_path,
        expected_documentation,
        "Endpoint documentation",
    )
    check_text(
        unity_asset_path,
        generate_contract.unity_asset(catalog),
        "Unity catalog asset",
    )
    check_text(
        service_definition_path,
        service_definition_asset(expected_manifest),
        "Unity service definition asset",
    )

    catalog_source = catalog.get("source")
    if not isinstance(catalog_source, Mapping):
        raise generate_contract.ContractError(
            "Generated catalog requires source metadata."
        )
    source_hashes = {
        str(catalog_source.get("sha256", "")),
        str(coverage.get("sourceSha256", "")),
        str(source.get("sha256", "")),
    }
    if len(source_hashes) != 1 or "" in source_hashes:
        raise generate_contract.ContractError(
            "Generated artifacts disagree about the source SHA-256."
        )
    endpoint_count = len(catalog.get("endpoints", []))
    if endpoint_count != coverage.get("catalogOperations"):
        raise generate_contract.ContractError(
            "Catalog and coverage operation counts do not match."
        )
    if not coverage.get("snapshotCoverageComplete"):
        raise generate_contract.ContractError(
            "Checked-in coverage is not complete for its supplied snapshot."
        )


def refresh_review_files(root: Path, source_revision: str) -> None:
    catalog = read_json(output_path(root, CATALOG), "Generated catalog")
    coverage = read_json(output_path(root, COVERAGE), "Generated coverage")
    manifest = generate_contract.contract_manifest(
        catalog,
        coverage,
        source_revision,
    )
    generate_contract.write_output(
        output_path(root, MANIFEST),
        generate_contract.encoded(manifest),
    )
    generate_contract.write_output(
        output_path(root, DOCUMENTATION),
        generate_contract.contract_documentation(
            catalog,
            coverage,
            manifest,
        ),
    )
    generate_contract.write_output(
        output_path(root, SERVICE_DEFINITION_ASSET),
        service_definition_asset(manifest),
    )


def run_generation(args: argparse.Namespace, source_revision: str) -> int:
    if args.spec is None:
        raise generate_contract.ContractError(
            "--spec is required unless validating generated artifacts."
        )
    output_root = args.output_root.resolve()
    command = [
        sys.executable,
        str(PACKAGE_ROOT / "Tools~" / "generate_contract.py"),
        "--spec",
        str(args.spec.resolve()),
        "--overlay",
        str(PACKAGE_ROOT / OVERLAY),
        "--catalog-out",
        str(output_path(output_root, CATALOG)),
        "--coverage-out",
        str(output_path(output_root, COVERAGE)),
        "--manifest-out",
        str(output_path(output_root, MANIFEST)),
        "--documentation-out",
        str(output_path(output_root, DOCUMENTATION)),
        "--unity-asset-out",
        str(output_path(output_root, UNITY_ASSET)),
        "--source-revision",
        source_revision,
        "--require-complete",
    ]
    if args.baseline_catalog is not None and args.baseline_catalog.is_file():
        command.extend(
            ["--baseline-catalog", str(args.baseline_catalog.resolve())]
        )
    if args.change_report_out is not None:
        command.extend(
            ["--change-report-out", str(args.change_report_out.resolve())]
        )
    if args.change_report_markdown_out is not None:
        command.extend(
            [
                "--change-report-markdown-out",
                str(args.change_report_markdown_out.resolve()),
            ]
        )
    if args.check:
        command.append("--check")
    completed = subprocess.run(command, cwd=PACKAGE_ROOT, check=False)
    if completed.returncode != 0:
        return completed.returncode

    manifest = read_json(
        output_path(output_root, MANIFEST),
        "Generated manifest",
    )
    expected_definition = service_definition_asset(manifest)
    definition_path = output_path(output_root, SERVICE_DEFINITION_ASSET)
    if args.check:
        check_text(
            definition_path,
            expected_definition,
            "Unity service definition asset",
        )
    else:
        generate_contract.write_output(
            definition_path,
            expected_definition,
        )
    return 0


def main() -> int:
    args = parse_args()
    try:
        selected_modes = sum(
            int(value)
            for value in (
                args.validate_generated,
                args.refresh_review_files,
            )
        )
        if selected_modes > 1:
            raise generate_contract.ContractError(
                "Choose only one generated-artifact maintenance mode."
            )
        if args.validate_generated:
            validate_generated(args.output_root)
            print("Generated Simultria contract artifacts are consistent.")
            return 0
        source_revision = require_source_revision(args.source_revision)
        if args.refresh_review_files:
            refresh_review_files(args.output_root, source_revision)
            print("Refreshed generated contract manifest and documentation.")
            return 0
        result = run_generation(args, source_revision)
        if result == 0:
            action = "Validated" if args.check else "Updated"
            print(f"{action} Simultria contract artifacts.")
        return result
    except generate_contract.ContractError as error:
        print(f"error: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
