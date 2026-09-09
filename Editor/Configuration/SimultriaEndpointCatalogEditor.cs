using System;
using Deucarian.API.Configuration;
using Deucarian.Editor;
using Deucarian.Simultria.API.Configuration;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    [CustomEditor(typeof(ApiEndpointCatalog))]
    internal sealed class SimultriaEndpointCatalogEditor : UnityEditor.Editor
    {
        private bool showPackageDetails;

        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>

            DeucarianEditorInspector.Create(OnInspectorGUI);


        public override void OnInspectorGUI()
        {
            var catalog = (ApiEndpointCatalog)target;
            if (!IsCanonicalPackageCatalog(catalog))
            {
                string path = AssetDatabase.GetAssetPath(catalog)
                    ?.Replace('\\', '/');
                bool simultriaOverride = string.Equals(
                    catalog.CatalogId,
                    SimultriaCatalogIds.ApiV2.Value,
                    StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(path) &&
                    path.StartsWith("Assets/", StringComparison.Ordinal);
                if (simultriaOverride)
                {
                    DeucarianEditorTextGUI.HelpBox(
                        "Project-owned Simultria contract override. Changes " +
                        "apply only to profiles that explicitly reference it.",
                        MessageType.Warning);
                }

                DrawDefaultInspector();
                return;
            }

            DeucarianEditorTextGUI.LabelField(
                "Simultria API v2 · package managed · read-only",
                DeucarianEditorWorkbenchGUI.BoldLabelStyle);
            DeucarianEditorTextGUI.LabelField(
                catalog.Endpoints.Count +
                " contract operations · no deployment URLs",
                DeucarianEditorWorkbenchGUI.MiniLabelStyle);
            DeucarianEditorTextGUI.HelpBox(
                "Configure environment URLs on a project-owned Simultria API " +
                "Profile. To customize routes or policies, create an explicit " +
                "project catalog override from that profile's Advanced section.",
                MessageType.Info);

            DrawGeneratedContractStatus();

            showPackageDetails = DeucarianEditorInputGUI.Foldout(
                showPackageDetails,
                "Contract details",
                true);
            if (showPackageDetails)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawDefaultInspector();
                }
            }
        }

        private static void DrawGeneratedContractStatus()
        {
            if (SimultriaContractUpdateService.TryLoadCurrentManifest(
                    out SimultriaContractManifestDocument manifest,
                    out _) &&
                manifest.source != null &&
                manifest.coverage != null)
            {
                using (new EditorGUILayout.VerticalScope(DeucarianEditorStyles.SectionBox))
                {
                    DeucarianEditorTextGUI.LabelField(
                        "Generated contract provenance",
                        DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    DeucarianEditorTextGUI.LabelField(
                        "Backend commit",
                        ShortValue(manifest.source.backendRevision, 16));
                    DeucarianEditorTextGUI.LabelField(
                        "Source SHA-256",
                        ShortValue(manifest.source.sha256, 20));
                    DeucarianEditorTextGUI.LabelField(
                        "Snapshot coverage",
                        manifest.coverage.snapshotCoverage ?? "Unknown");
                }
            }
            else
            {
                DeucarianEditorTextGUI.HelpBox(
                    "Generated contract provenance is missing. Validate the " +
                    "package before release.",
                    MessageType.Warning);
            }

            if (DeucarianEditorButtons.Secondary(
                    "Open Contract Updater",
                    true,
                    GUILayout.Height(26f)))
            {
                SimultriaContractUpdateWindow.OpenWindow();
            }
        }

        private static string ShortValue(string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= length)
            {
                return value ?? "Unknown";
            }

            return value.Substring(0, length) + "…";
        }

        internal static bool IsCanonicalPackageCatalog(
            ApiEndpointCatalog catalog)
        {
            if (catalog == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(catalog)
                ?.Replace('\\', '/');
            return string.Equals(
                path,
                SimultriaApiDefinitionDefaults.EndpointCatalogAssetPath,
                StringComparison.Ordinal);
        }
    }
}
