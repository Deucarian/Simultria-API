using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaApiProfileAssetFactory
    {
        internal const string CreateMenuPath =
            "Assets/Create/Deucarian/Simultria/API Profile";

        internal const string CreateLegacyMenuPath =
            "Assets/Create/Deucarian/Simultria/Advanced/Legacy API Profile " +
            "(Compatibility)";

        internal const string CreateCatalogOverrideMenuPath =
            "Assets/Create/Deucarian/Simultria/Advanced/" +
            "API v2 Contract Override";

        [MenuItem(CreateMenuPath, false, 201)]
        internal static void CreateFromMenu()
        {
            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaApiConnectionProfile.asset");
            if (!TryCreateProjectConnectionProfile(
                    path,
                    out ApiConnectionProfile profile,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria API Connection Profile",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(profile);
        }

        [MenuItem(CreateLegacyMenuPath, false, 202)]
        private static void CreateLegacyFromMenu()
        {
            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaApiProfile.Legacy.asset");
            if (!TryCreateProjectProfile(
                    path,
                    out SimultriaApiProfile profile,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Legacy Simultria API Profile",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(profile);
        }

        [MenuItem(CreateCatalogOverrideMenuPath, false, 203)]
        private static void CreateCatalogOverrideFromMenu()
        {
            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaApiV2EndpointCatalog.Override.asset");
            if (!TryCreateCatalogOverrideAsset(
                    path,
                    out ApiEndpointCatalog endpointCatalog,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria API Contract Override",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(endpointCatalog);
        }

        internal static bool TryCreateProjectConnectionProfile(
            string assetPath,
            out ApiConnectionProfile profile,
            out string error)
        {
            profile = null;
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

            ApiEndpointCatalog endpointCatalog =
                AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogAssetPath);
            if (!SimultriaApiConnectionProfileAdapter.IsCompatibleCatalog(
                    endpointCatalog,
                    out error))
            {
                return false;
            }

            var environments = new List<ApiEnvironmentProfile>();
            bool createdRootAsset = false;
            try
            {
                foreach (ApiEnvironmentDescriptor descriptor in
                    SimultriaEnvironmentDescriptors.Standard)
                {
                    environments.Add(CreateEnvironment(descriptor));
                }

                profile = ApiConnectionProfile.CreateTransient(
                    environments,
                    endpointCatalog,
                    SimultriaEnvironmentDescriptors.Standard);
                profile.name = Path.GetFileNameWithoutExtension(
                    normalizedPath);
                AssetDatabase.CreateAsset(profile, normalizedPath);
                createdRootAsset = true;
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    AssetDatabase.AddObjectToAsset(environment, profile);
                }

                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    normalizedPath,
                    ImportAssetOptions.ForceUpdate);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                if (createdRootAsset)
                {
                    AssetDatabase.DeleteAsset(normalizedPath);
                }

                DestroyTransient(profile);
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    DestroyTransient(environment);
                }

                profile = null;
                error =
                    "The project-owned Simultria API connection profile could " +
                    "not be created (" + exception.GetType().Name + ").";
                return false;
            }
        }

        internal static bool TryCreateProjectProfile(
            string assetPath,
            out SimultriaApiProfile profile,
            out string error)
        {
            profile = null;
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

            ApiEndpointCatalog endpointCatalog =
                AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogAssetPath);
            if (!SimultriaApiConnectionProfileAdapter.IsCompatibleCatalog(
                    endpointCatalog,
                    out error))
            {
                return false;
            }

            var environments = new List<ApiEnvironmentProfile>();
            bool createdRootAsset = false;
            try
            {
                foreach (ApiEnvironmentDescriptor descriptor in
                    SimultriaEnvironmentDescriptors.Standard)
                {
                    environments.Add(CreateEnvironment(descriptor));
                }

                profile = SimultriaApiProfile.CreateTransient(
                    environments,
                    endpointCatalog);
                profile.name = Path.GetFileNameWithoutExtension(
                    normalizedPath);
                AssetDatabase.CreateAsset(profile, normalizedPath);
                createdRootAsset = true;
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    AssetDatabase.AddObjectToAsset(environment, profile);
                }

                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    normalizedPath,
                    ImportAssetOptions.ForceUpdate);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                if (createdRootAsset)
                {
                    AssetDatabase.DeleteAsset(normalizedPath);
                }

                DestroyTransient(profile);
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    DestroyTransient(environment);
                }

                profile = null;
                error =
                    "The project-owned Simultria API profile could not be " +
                    "created (" + exception.GetType().Name + ").";
                return false;
            }
        }

        internal static bool TryCreateProjectCatalogOverride(
            SimultriaApiProfile profile,
            string assetPath,
            out ApiEndpointCatalog endpointCatalog,
            out string error)
        {
            endpointCatalog = null;
            string profilePath = AssetDatabase.GetAssetPath(profile)
                ?.Replace('\\', '/');
            if (profile == null || string.IsNullOrWhiteSpace(profilePath) ||
                !profilePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                error =
                    "Catalog overrides can only be created for a " +
                    "project-owned Simultria API profile.";
                return false;
            }

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

            if (!TryCreateCatalogOverrideAsset(
                    normalizedPath,
                    out endpointCatalog,
                    out error))
            {
                return false;
            }

            try
            {
                Undo.RecordObject(profile, "Use Simultria catalog override");
                var serializedProfile = new SerializedObject(profile);
                serializedProfile.FindProperty("endpointCatalog")
                    .objectReferenceValue = endpointCatalog;
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    normalizedPath,
                    ImportAssetOptions.ForceUpdate);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                AssetDatabase.DeleteAsset(normalizedPath);
                endpointCatalog = null;
                error =
                    "The project-owned endpoint catalog override could not " +
                    "be created (" + exception.GetType().Name + ").";
                return false;
            }
        }

        internal static bool TryCreateCatalogOverrideAsset(
            string assetPath,
            out ApiEndpointCatalog endpointCatalog,
            out string error)
        {
            endpointCatalog = null;
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

            ApiEndpointCatalog packageCatalog =
                AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogAssetPath);
            if (!SimultriaApiConnectionProfileAdapter.IsCompatibleCatalog(
                    packageCatalog,
                    out error))
            {
                return false;
            }

            try
            {
                endpointCatalog = UnityEngine.Object.Instantiate(packageCatalog);
                endpointCatalog.name = Path.GetFileNameWithoutExtension(
                    normalizedPath);
                AssetDatabase.CreateAsset(endpointCatalog, normalizedPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    normalizedPath,
                    ImportAssetOptions.ForceUpdate);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                AssetDatabase.DeleteAsset(normalizedPath);
                DestroyTransient(endpointCatalog);
                endpointCatalog = null;
                error =
                    "The project-owned endpoint catalog override could not " +
                    "be created (" + exception.GetType().Name + ").";
                return false;
            }
        }

        internal static bool TryAssignEndpointCatalog(
            SimultriaApiProfile profile,
            ApiEndpointCatalog endpointCatalog,
            out string error)
        {
            if (profile == null || endpointCatalog == null)
            {
                error = "Choose a Simultria endpoint catalog.";
                return false;
            }

            string profilePath = AssetDatabase.GetAssetPath(profile)
                ?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(profilePath) ||
                !profilePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                error =
                    "Only a project-owned Simultria API profile can select a " +
                    "catalog override.";
                return false;
            }

            ApiEndpointCatalog packageCatalog =
                AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogAssetPath);
            string catalogPath = AssetDatabase.GetAssetPath(endpointCatalog)
                ?.Replace('\\', '/');
            bool packageManaged = endpointCatalog == packageCatalog;
            bool projectOwned = !string.IsNullOrWhiteSpace(catalogPath) &&
                catalogPath.StartsWith("Assets/", StringComparison.Ordinal);
            if (!packageManaged && !projectOwned)
            {
                error =
                    "Use the package-managed Simultria contract or an " +
                    "explicit project-owned catalog override.";
                return false;
            }

            if (!SimultriaApiConnectionProfileAdapter.IsCompatibleCatalog(
                    endpointCatalog,
                    out string validationMessage))
            {
                error = validationMessage;
                return false;
            }

            Undo.RecordObject(profile, "Select Simultria endpoint catalog");
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("endpointCatalog")
                .objectReferenceValue = endpointCatalog;
            serializedProfile.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            error = null;
            return true;
        }

        private static ApiEnvironmentProfile CreateEnvironment(
            ApiEnvironmentDescriptor descriptor)
        {
            var environment =
                ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
            environment.name = descriptor.DisplayName;
            environment.EnvironmentId = descriptor.EnvironmentId.Value;
            environment.DisplayName = descriptor.DisplayName;
            environment.Clients.Add(
                new ApiNamedClientDefinition
                {
                    ClientId = SimultriaClientIds.Primary.Value,
                    BaseUrl = string.Empty
                });
            return environment;
        }

        private static bool TryNormalizeProjectAssetPath(
            string assetPath,
            out string normalizedPath,
            out string error)
        {
            normalizedPath = assetPath?.Trim().Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(normalizedPath) ||
                !normalizedPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal) ||
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

            const string resourcesMarker = "/Resources/";
            int resourcesIndex = normalizedPath.IndexOf(
                resourcesMarker,
                StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
            {
                string resourcePath = normalizedPath.Substring(
                    resourcesIndex + resourcesMarker.Length);
                resourcePath = resourcePath.Substring(
                    0,
                    resourcePath.Length - Path.GetExtension(resourcePath).Length);
                bool shadowsProfile = string.Equals(
                    resourcePath,
                    SimultriaApiProfileDefaults.DefaultProfileResourcePath,
                    StringComparison.OrdinalIgnoreCase);
                bool shadowsCatalog = string.Equals(
                    resourcePath,
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogResourcePath,
                    StringComparison.OrdinalIgnoreCase);
                if (shadowsProfile || shadowsCatalog)
                {
                    error =
                        "This path would shadow a package-provided Simultria " +
                        "resource. Choose a different project asset path.";
                    normalizedPath = null;
                    return false;
                }
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
                   (string.Equals(
                        selectedPath,
                        "Assets",
                        StringComparison.Ordinal) ||
                    selectedPath.StartsWith(
                        "Assets/",
                        StringComparison.Ordinal))
                ? selectedPath
                : "Assets";
        }

        private static void DestroyTransient(UnityEngine.Object value)
        {
            if (value != null && !AssetDatabase.Contains(value))
            {
                Undo.DestroyObjectImmediate(value);
            }
        }
    }
}
