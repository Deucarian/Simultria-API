using System;
using System.IO;
using Deucarian.API.Configuration;
using Deucarian.API.Editor;
using Deucarian.Simultria.API.Configuration;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaConnectionSettingsAssetFactory
    {
        internal const string CreateMenuPath =
            "Assets/Create/Deucarian/Connections/Simultria Connection Settings";

        internal const string CreateDefinitionOverrideMenuPath =
            "Assets/Create/Deucarian/Connections/Advanced/" +
            "Simultria API Definition Override";

        [MenuItem(CreateMenuPath, false, 200)]
        internal static void CreateFromMenu()
        {
            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaConnectionSettings.asset");
            if (!TryCreateProjectConnectionSettings(
                    path,
                    out ApiConnectionSettings settings,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria Connection Settings",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(settings);
        }

        [MenuItem(CreateDefinitionOverrideMenuPath, false, 220)]
        private static void CreateDefinitionOverrideFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Fork Simultria API Definition",
                    "This creates a project-owned contract fork. Package " +
                    "updates will no longer change the fork automatically.",
                    "Create Override",
                    "Cancel"))
            {
                return;
            }

            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaApiV2Definition.Override.asset");
            if (!TryCreateDefinitionOverrideAsset(
                    path,
                    out ApiServiceDefinition definition,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria API Definition Override",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(definition);
        }

        internal static bool TryCreateProjectConnectionSettings(
            string assetPath,
            out ApiConnectionSettings settings,
            out string error)
        {
            ApiServiceDefinition definition =
                LoadPackageServiceDefinition();
            if (!SimultriaApiConnectionSettingsAdapter.IsCompatibleDefinition(
                    definition,
                    out error))
            {
                settings = null;
                return false;
            }

            return ApiConnectionSettingsAssetFactory.TryCreateProjectSettings(
                assetPath,
                definition,
                out settings,
                out error);
        }

        internal static bool TryCreateDefinitionOverrideAsset(
            string assetPath,
            out ApiServiceDefinition definition,
            out string error)
        {
            definition = null;
            if (!TryNormalizeProjectAssetPath(
                    assetPath,
                    out string normalizedPath,
                    out error))
            {
                return false;
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                error = "An asset already exists at the selected path.";
                return false;
            }

            ApiServiceDefinition packageDefinition =
                LoadPackageServiceDefinition();
            if (!SimultriaApiConnectionSettingsAdapter.IsCompatibleDefinition(
                    packageDefinition,
                    out error))
            {
                return false;
            }

            bool rootCreated = false;
            try
            {
                definition = UnityEngine.Object.Instantiate(packageDefinition);
                definition.name = Path.GetFileNameWithoutExtension(normalizedPath);
                ApiEndpointCatalog catalog = UnityEngine.Object.Instantiate(
                    packageDefinition.EndpointCatalog);
                catalog.name = "Endpoint Catalog";
                definition.EndpointCatalog = catalog;
                AssetDatabase.CreateAsset(definition, normalizedPath);
                rootCreated = true;
                AssetDatabase.AddObjectToAsset(catalog, definition);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    normalizedPath,
                    ImportAssetOptions.ForceUpdate);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                if (rootCreated)
                {
                    AssetDatabase.DeleteAsset(normalizedPath);
                }

                if (definition != null && !AssetDatabase.Contains(definition))
                {
                    Undo.DestroyObjectImmediate(definition);
                }

                definition = null;
                error = "The definition override could not be created (" +
                    exception.GetType().Name + ").";
                return false;
            }
        }

        private static ApiServiceDefinition LoadPackageServiceDefinition()
        {
            return AssetDatabase.LoadAssetAtPath<ApiServiceDefinition>(
                SimultriaApiDefinitionDefaults.ServiceDefinitionAssetPath);
        }

        private static bool TryNormalizeProjectAssetPath(
            string assetPath,
            out string normalizedPath,
            out string error)
        {
            normalizedPath = assetPath?.Trim().Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(normalizedPath) ||
                !normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(
                    Path.GetExtension(normalizedPath),
                    ".asset",
                    StringComparison.OrdinalIgnoreCase))
            {
                error =
                    "Choose a new .asset path inside this project's Assets folder.";
                normalizedPath = null;
                return false;
            }

            error = null;
            return true;
        }

        private static string ResolveSelectedProjectDirectory()
        {
            string selectedPath = AssetDatabase.GetAssetPath(
                Selection.activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return "Assets";
            }

            selectedPath = selectedPath.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                selectedPath = Path.GetDirectoryName(selectedPath)
                    ?.Replace('\\', '/');
            }

            return !string.IsNullOrWhiteSpace(selectedPath) &&
                (string.Equals(selectedPath, "Assets", StringComparison.Ordinal) ||
                 selectedPath.StartsWith("Assets/", StringComparison.Ordinal))
                ? selectedPath
                : "Assets";
        }
    }
}
