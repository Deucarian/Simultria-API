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
                    EditorGUILayout.HelpBox(
                        "Project-owned Simultria contract override. Changes " +
                        "apply only to profiles that explicitly reference it.",
                        MessageType.Warning);
                }

                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.LabelField(
                "Simultria API v2 · package managed · read-only",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                catalog.Endpoints.Count +
                " contract operations · no deployment URLs",
                EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Configure environment URLs on a project-owned Simultria API " +
                "Profile. To customize routes or policies, create an explicit " +
                "project catalog override from that profile's Advanced section.",
                MessageType.Info);

            DrawGeneratedContractStatus();

            showPackageDetails = EditorGUILayout.Foldout(
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
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        "Generated contract provenance",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        "Backend commit",
                        ShortValue(manifest.source.backendRevision, 16));
                    EditorGUILayout.LabelField(
                        "Source SHA-256",
                        ShortValue(manifest.source.sha256, 20));
                    EditorGUILayout.LabelField(
                        "Snapshot coverage",
                        manifest.coverage.snapshotCoverage ?? "Unknown");
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
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
