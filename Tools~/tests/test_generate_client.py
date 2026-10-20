from copy import deepcopy
import json
from pathlib import Path
import re
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import generate_client as client
import generate_contract as contract
from client_render import render
from client_schema import clean_schema, Models


class TypedClientTests(unittest.TestCase):
    def setUp(self):
        self.spec = {
            "openapi": "3.0.3", "paths": {
                "/api/v2/widgets/{id}": {
                    "parameters": [
                        {"name": "id", "in": "path", "required": True, "schema": {"type": "integer"}},
                        {"name": "tenant", "in": "path", "required": True, "schema": {"type": "string"}}],
                    "get": {"operationId": "getWidget", "tags": ["Widgets"],
                            "parameters": [{"name": "include", "in": "query", "schema": {"type": "boolean"}}],
                            "responses": {"200": {"content": {"application/json": {"schema": {
                                "type": "object", "properties": {"name": {"type": "string"},
                                "items": {"type": "array"}, "42": {"type": "integer"}}}}}}}}}}
        }
        self.overlay = {"catalogId": "simultria.api-v2", "displayName": "Test",
                        "defaultClientId": "simultria.primary", "operations": {}}

    def description(self):
        catalog, _ = contract.generate(self.spec, self.overlay, Path("openapi.yaml"))
        return client.build(self.spec, catalog), catalog

    def test_source_routes_types_and_numeric_property_names_are_preserved(self):
        description, _ = self.description()
        operation = description["operations"][0]
        self.assertEqual(["id", "include"], [p["name"] for p in operation["parameters"]])
        files = render(description)
        source = files["Operations/Widgets/GetWidget.g.cs"]
        self.assertIn("int id,", source)
        self.assertIn('WithPathParameter("id",', source)
        self.assertIn('QueryParameters["include"]', source)
        dto = files["Models/WidgetsGetWidgetResponse.g.cs"]
        self.assertIn('JsonProperty("42")', dto)
        self.assertIn("List<JToken> Items", dto)
        self.assertEqual(files, render(json.loads(json.dumps(description))))
        self.assertEqual({"properties": {"42": {"type": "integer"}}},
                         clean_schema({"properties": {42: {"type": "integer"}}}, {}))

    def test_source_fingerprint_mismatch_fails(self):
        _, catalog = self.description()
        self.spec["paths"]["/api/v2/widgets/{id}"]["get"]["operationId"] = "changed"
        with self.assertRaisesRegex(contract.ContractError, "does not match"):
            client.build(self.spec, catalog)

    def test_examples_descriptions_and_servers_never_enter_client_contract(self):
        self.spec["servers"] = [{"url": "https://private.invalid"}]
        schema = self.spec["paths"]["/api/v2/widgets/{id}"]["get"]["responses"]["200"]["content"]["application/json"]["schema"]
        schema["example"] = {"token": "secret-example"}
        schema["description"] = "secret-description"
        description, _ = self.description()
        encoded = json.dumps(description)
        self.assertNotIn("secret", encoded)
        self.assertNotIn("private.invalid", encoded)

    def test_missing_path_schema_fails_instead_of_guessing_type(self):
        self.spec["paths"]["/api/v2/widgets/{id}"]["parameters"] = []
        with self.assertRaisesRegex(contract.ContractError, "Missing schema"):
            self.description()

    def test_missing_response_schema_is_explicit_json_token(self):
        self.spec["paths"]["/api/v2/widgets/{id}"]["get"]["responses"] = {}
        description, _ = self.description()
        self.assertIn("ApiResult<JToken>", render(description)["Operations/Widgets/GetWidget.g.cs"])

    def test_unsupported_query_encoding_fails_before_emission(self):
        self.spec["paths"]["/api/v2/widgets/{id}"]["get"]["parameters"][0]["schema"] = {"type": "array"}
        with self.assertRaisesRegex(contract.ContractError, "Unsupported parameter"):
            self.description()

    def test_local_schema_references_resolve_without_network(self):
        spec = {"components": {"schemas": {"Thing": {"type": "string"}}}}
        self.assertEqual({"type": "string"}, clean_schema({"$ref": "#/components/schemas/Thing"}, spec))
        with self.assertRaisesRegex(contract.ContractError, "Only local"):
            clean_schema({"$ref": "https://example.invalid/schema"}, spec)

    def test_recursive_schemas_fail_with_an_actionable_message(self):
        spec = {"components": {"schemas": {"Thing": {"$ref": "#/components/schemas/Thing"}}}}
        with self.assertRaisesRegex(contract.ContractError, "Recursive"):
            clean_schema(spec["components"]["schemas"]["Thing"], spec)

    def test_unspecified_body_fields_are_omitted_and_explicit_null_has_a_presence_flag(self):
        models = Models()
        models.type_name({"type": "object", "properties": {"name": {"type": "string", "nullable": True}}}, "PatchBody", True)
        source = models.files["Models/PatchBody.g.cs"]
        self.assertIn("_nameSpecified = true", source)
        self.assertIn("ShouldSerializeName() => _nameSpecified", source)
        self.assertNotIn("NullValueHandling.Ignore", source)

    def test_multipart_file_is_typed_and_mapped_to_form_sections(self):
        operation = self.spec["paths"]["/api/v2/widgets/{id}"]["get"]
        operation["requestBody"] = {"required": True, "content": {"multipart/form-data": {"schema": {
            "type": "object", "properties": {"file": {"type": "string", "format": "binary"}}}}}}
        description, _ = self.description()
        files = render(description)
        self.assertIn("SimultriaUpload File", files["Models/WidgetsGetWidgetBody.g.cs"])
        self.assertIn("ApiRequestBodyFormat.MultipartForm", files["Operations/Widgets/GetWidget.g.cs"])

    def test_generation_check_detects_drift_and_prunes_removed_generated_operations(self):
        description, catalog = self.description()
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            client.sync(root, description)
            client.validate(root, catalog)
            source = root / client.GENERATED / "Operations/Widgets/GetWidget.g.cs"
            source.write_text(source.read_text() + "// modified\n")
            with self.assertRaises(contract.ContractError):
                client.validate(root, catalog)
            client.sync(root, description)
            removed = deepcopy(description)
            removed["operations"] = []
            client.sync(root, removed)
            self.assertFalse(source.exists())
            self.assertFalse(Path(str(source) + ".meta").exists())

    def test_stable_endpoint_uses_friendly_name(self):
        self.overlay["operations"] = {"getWidget": {"endpointId": "simultria.activities.list"}}
        description, _ = self.description()
        self.assertEqual("ModelVersions", description["operations"][0]["group"])
        self.assertEqual("GetActivities", description["operations"][0]["name"])

    def test_directory_discovery_preserves_its_package_owned_host(self):
        description, _ = self.description()
        operation = description["operations"][0]
        operation.update(id="simultria.unity-builds.version", group="UnityBuildVersions", name="GetVersion")
        operation["parameters"] = [{"name": n, "in": "path", "required": True, "schema": {"type": "string"}}
                                   for n in ("id", "product")]
        source = render(description)["Operations/UnityBuildVersions/GetVersion.g.cs"]
        self.assertIn("SimultriaUnityBuildDirectory.CreateEndpoint", source)
        self.assertNotIn("WithPathParameter", source)

    def test_new_operation_generates_typed_callbacks_for_request_and_path_overloads(self):
        description, _ = self.description()
        source = render(description)["Operations/Widgets/GetWidget.g.cs"]
        self.assertRegex(source, r"public void GetWidget\(WidgetsGetWidgetRequest parameters,\s*"
                                 r"Action<WidgetsGetWidgetResponse> onSuccess,\s*Action<ApiError> onFailure,")
        self.assertRegex(source, r"public void GetWidget\(int id,\s*Action<WidgetsGetWidgetResponse> onSuccess,")
        self.assertIn("_api.Send(SimultriaEndpoints.Widgets.GetWidget,", source)
        self.assertIn("GetWidget(parameters, onSuccess, onFailure, cancellationToken);", source)
        self.assertEqual(2, source.count("public void GetWidget("))

    def test_optional_and_empty_requests_get_callback_only_overload_without_ambiguity(self):
        description, _ = self.description()
        operation = description["operations"][0]
        operation["parameters"] = [p for p in operation["parameters"] if p["in"] != "path"]
        for parameters in (operation["parameters"], []):
            operation["parameters"] = parameters
            source = render(description)["Operations/Widgets/GetWidget.g.cs"]
            self.assertRegex(source, r"public void GetWidget\(Action<WidgetsGetWidgetResponse> onSuccess,")
            self.assertRegex(source, r"public void GetWidget\(WidgetsGetWidgetRequest parameters,")
            self.assertNotRegex(source, r"public void GetWidget\(WidgetsGetWidgetRequest parameters =")
            self.assertIn("parameters ?? new WidgetsGetWidgetRequest()", source)
            self.assertEqual(2, source.count("public void GetWidget("))

    def test_required_body_does_not_gain_no_argument_callback_or_drop_upload_mapping(self):
        operation = self.spec["paths"]["/api/v2/widgets/{id}"]["get"]
        operation["requestBody"] = {"required": True, "content": {"multipart/form-data": {"schema": {
            "type": "object", "properties": {"file": {"type": "string", "format": "binary"}}}}}}
        description, _ = self.description()
        source = render(description)["Operations/Widgets/GetWidget.g.cs"]
        self.assertEqual(1, source.count("public void GetWidget("))
        self.assertIn("Action<WidgetsGetWidgetResponse> onSuccess", source)
        self.assertIn("ApiRequestBodyFormat.MultipartForm", source)

    def test_every_snapshot_async_overload_has_matching_typed_callback(self):
        root = Path(__file__).resolve().parents[2]
        description = json.loads((root / client.CONTRACT).read_text(encoding="utf-8"))
        files = render(description)
        self.assertGreater(len(description["operations"]), 0)
        for operation in description["operations"]:
            name, group = operation["name"], operation["group"]
            with self.subTest(group=group, operation=name):
                source = files[f"Operations/{group}/{name}.g.cs"]
                response = re.search(r"Task<ApiResult<(.+)>> " + re.escape(name) + r"Async\(", source).group(1)
                async_count = source.count(f"public Task<ApiResult<{response}>> {name}Async(")
                optional = not any(p["required"] for p in operation["parameters"]) and not operation["bodyRequired"]
                callback_count = source.count(f"public void {name}(")
                self.assertEqual(async_count + int(optional), callback_count)
                self.assertEqual(callback_count, source.count(f"Action<{response}> onSuccess"))
                self.assertEqual(callback_count, source.count("Action<ApiError> onFailure"))
                self.assertEqual(1, len(re.findall(r"_api\.Send\(\s*SimultriaEndpoints\." + re.escape(group)
                                                   + r"\." + re.escape(name) + ",", source)))
