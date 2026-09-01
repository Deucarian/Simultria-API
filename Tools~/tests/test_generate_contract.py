from __future__ import annotations

import json
from pathlib import Path
import sys
import tempfile
import unittest


TOOLS_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS_ROOT))

import generate_contract  # noqa: E402
import update_contract  # noqa: E402


class GenerateContractTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.spec_path = self.root / "openapi.json"
        self.spec = {
            "openapi": "3.0.3",
            "security": [{"passport": []}],
            "paths": {
                "/api/v2/login": {
                    "post": {
                        "operationId": "login",
                        "security": [],
                    }
                },
                "/api/v2/projects": {
                    "get": {"operationId": "indexProjects"}
                },
            },
        }
        self.overlay = {
            "catalogId": "simultria.api-v2",
            "displayName": "Simultria API v2",
            "defaultClientId": "simultria.primary",
            "operations": {
                "login": {
                    "endpointId": "simultria.auth.login",
                    "authentication": "Disabled",
                    "suppressLogging": True,
                }
            },
        }
        self.write_spec()

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def write_spec(self) -> None:
        self.spec_path.write_text(
            json.dumps(self.spec),
            encoding="utf-8",
        )

    def generate(self):
        return generate_contract.generate(
            self.spec,
            self.overlay,
            self.spec_path,
        )

    def test_generation_emits_complete_safe_catalog_and_review_files(self):
        catalog, coverage = self.generate()
        manifest = generate_contract.contract_manifest(
            catalog,
            coverage,
            "53f2ee778c5ec3d22763c86537850061642317cb",
        )
        documentation = generate_contract.contract_documentation(
            catalog,
            coverage,
            manifest,
        )

        self.assertEqual(2, coverage["operationsInSuppliedSnapshot"])
        self.assertTrue(coverage["snapshotCoverageComplete"])
        self.assertEqual(1, manifest["catalog"]["reviewedStableOperationCount"])
        self.assertEqual(1, manifest["catalog"]["generatedOperationCount"])
        self.assertIn("Reviewed stable endpoints", documentation)
        self.assertIn("simultria.auth.login", documentation)
        self.assertIn("simultria.generated.get.api.v2.projects", documentation)
        generated = next(
            endpoint
            for endpoint in catalog["endpoints"]
            if endpoint["endpointId"].startswith("simultria.generated.")
        )
        self.assertTrue(generated["suppressLogging"])

    def test_service_definition_uses_first_class_local_stage(self):
        manifest = {
            "source": {
                "backendRevision": "53f2ee778c5ec3d22763c86537850061642317cb",
                "sha256": "0" * 64,
            }
        }

        asset = update_contract.service_definition_asset(manifest)

        self.assertIn(
            "environmentId: simultria.local\n    stage: 5",
            asset,
        )
        self.assertNotIn(
            "environmentId: simultria.local\n    stage: 0",
            asset,
        )

    def test_change_report_separates_additions_from_sensitive_changes(self):
        baseline, _ = self.generate()
        self.spec["paths"]["/api/v2/projects"]["get"]["security"] = []
        self.spec["paths"]["/api/v2/models"] = {
            "get": {"operationId": "indexModels"}
        }
        self.write_spec()
        generated, _ = self.generate()

        report = generate_contract.compare_catalogs(baseline, generated)

        self.assertEqual(1, report["summary"]["added"])
        self.assertEqual(1, report["summary"]["changed"])
        self.assertTrue(report["reviewRequired"])
        self.assertTrue(report["breakingOrSecurityReviewRequired"])
        changed_fields = {
            change["field"]
            for change in report["changed"][0]["changes"]
        }
        self.assertIn("authentication", changed_fields)

    def test_source_revision_rejects_branch_names_and_accepts_git_commits(self):
        with self.assertRaises(generate_contract.ContractError):
            generate_contract.normalized_source_revision("development")

        self.assertEqual(
            "53f2ee7",
            generate_contract.normalized_source_revision("53F2EE7"),
        )

    def test_source_fingerprint_ignores_volatile_examples_only(self):
        baseline = generate_contract.contract_source_hash(self.spec)
        self.spec["paths"]["/api/v2/login"]["post"]["example"] = {
            "token": "generated-value"
        }
        self.assertEqual(
            baseline,
            generate_contract.contract_source_hash(self.spec),
        )

        self.spec["paths"]["/api/v2/login"]["post"]["security"] = [
            {"passport": []}
        ]
        self.assertNotEqual(
            baseline,
            generate_contract.contract_source_hash(self.spec),
        )

    def test_unsupported_openapi_method_fails_closed(self):
        self.spec["paths"]["/api/v2/projects"]["head"] = {
            "operationId": "headProjects"
        }
        self.write_spec()

        with self.assertRaises(generate_contract.ContractError):
            self.generate()


if __name__ == "__main__":
    unittest.main()
