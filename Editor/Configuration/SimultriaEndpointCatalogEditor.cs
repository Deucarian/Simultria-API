using System;
using Deucarian.API.Configuration;
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
                SimultriaApiProfileDefaults.DefaultEndpointCatalogAssetPath,
                StringComparison.Ordinal);
        }
    }
}
