using System;
using Deucarian.API.Configuration;
using Deucarian.Editor;
using Deucarian.Simultria.API.Configuration;
using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.Simultria.API.Editor
{
    [CustomEditor(typeof(ApiEndpointCatalog))]
    internal sealed class SimultriaEndpointCatalogEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var catalog = (ApiEndpointCatalog)target;
            var root = DeucarianEditorInspector.CreateToolkit("API endpoint catalog");
            if (!IsCanonicalPackageCatalog(catalog))
            {
                string path = AssetDatabase.GetAssetPath(catalog)?.Replace('\\', '/');
                if (string.Equals(catalog.CatalogId, SimultriaCatalogIds.ApiV2.Value, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets/", StringComparison.Ordinal))
                    root.Add(new HelpBox("Project-owned Simultria override. Only profiles that reference it use these changes.", HelpBoxMessageType.Warning));
                DeucarianEditorInspector.Properties(root, serializedObject);
                return root;
            }

            root.Add(DeucarianEditorWorkspaceControls.Label(
                "Simultria API v2 · " + catalog.Endpoints.Count + " operations · read-only", "dw-muted"));
            root.Add(DeucarianEditorWorkspaceControls.Label(
                "Set deployment URLs on a project-owned API Profile. Create an override from its Advanced section to customize this contract.", "dw-muted"));
            if (SimultriaContractUpdateService.TryLoadCurrentManifest(out var manifest, out _) &&
                manifest.source != null && manifest.coverage != null)
            {
                var provenance = new DeucarianEditorWorkspaceForm(root);
                provenance.ReadOnly("backend-commit", "Backend commit", () => ShortValue(manifest.source.backendRevision, 16));
                provenance.ReadOnly("source-hash", "Source SHA-256", () => ShortValue(manifest.source.sha256, 20));
                provenance.ReadOnly("coverage", "Snapshot coverage", () => manifest.coverage.snapshotCoverage ?? "Unknown");
            }
            else root.Add(new HelpBox("Contract provenance is missing. Validate the package before release.", HelpBoxMessageType.Warning));
            root.Add(DeucarianEditorWorkspaceControls.Button("Open contract updater", () => SimultriaContractUpdateWindow.OpenWindow()));
            var details = new Foldout { text = "Contract details", value = false };
            details.AddToClassList("dw-foldout"); root.Add(details);
            DeucarianEditorInspector.Properties(details, serializedObject).SetEnabled(false);
            return root;
        }

        private static string ShortValue(string value, int length) =>
            string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Length <= length ? value : value.Substring(0, length) + "…";

        internal static bool IsCanonicalPackageCatalog(ApiEndpointCatalog catalog)
        {
            if (catalog == null) return false;
            string path = AssetDatabase.GetAssetPath(catalog)?.Replace('\\', '/');
            return string.Equals(path, SimultriaApiDefinitionDefaults.EndpointCatalogAssetPath, StringComparison.Ordinal);
        }
    }
}
