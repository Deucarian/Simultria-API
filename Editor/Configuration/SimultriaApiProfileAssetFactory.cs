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

        [MenuItem(CreateMenuPath, false, 201)]
        private static void CreateFromMenu()
        {
            string directory = ResolveSelectedProjectDirectory();
            string path = AssetDatabase.GenerateUniqueAssetPath(
                directory + "/SimultriaApiProfile.asset");
            if (!TryCreateProjectProfile(
                    path,
                    out SimultriaApiProfile profile,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria API Profile",
                    error,
                    "OK");
                return;
            }

            ProjectWindowUtil.ShowCreatedAsset(profile);
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
            if (endpointCatalog == null)
            {
                error =
                    "The package-provided Simultria endpoint catalog is missing.";
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
                if (string.Equals(
                        resourcePath,
                        SimultriaApiProfileDefaults.DefaultProfileResourcePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    error =
                        "This path would shadow the package-provided Simultria " +
                        "profile. Choose a different project asset path.";
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
