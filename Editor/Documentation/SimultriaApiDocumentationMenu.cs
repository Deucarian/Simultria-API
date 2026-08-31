using System;
using System.IO;
using Deucarian.Simultria.API.Endpoints;
using UnityEditor;
using UnityEngine;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaApiDocumentationMenu
    {
        private const string DeveloperGuideRelativePath =
            "Documentation~/index.md";
        private const string ReadmeRelativePath = "README.md";
        private const string EndpointReferenceRelativePath =
            "Documentation~/Generated/API-Endpoints.md";

        internal static void OpenDocumentation()
        {
            if (!TryFindDocumentation(out string path))
            {
                ShowMissingDocumentationDialog("documentation");
                return;
            }

            Application.OpenURL(new Uri(path).AbsoluteUri);
        }

        internal static void OpenEndpointReference()
        {
            OpenDocumentationFile(
                EndpointReferenceRelativePath,
                "generated endpoint reference");
        }

        internal static bool TryFindDocumentationFile(
            string packageRoot,
            string relativePath,
            out string path)
        {
            path = string.Empty;
            if (string.IsNullOrWhiteSpace(packageRoot) ||
                string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            string root = Path.GetFullPath(packageRoot)
                .TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            string candidate = Path.GetFullPath(
                Path.Combine(root, relativePath));
            string rootPrefix = root + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(candidate))
            {
                return false;
            }

            path = candidate;
            return true;
        }

        internal static bool TryFindDocumentation(out string path)
        {
            path = string.Empty;
            PackageManagerPackageInfo package =
                PackageManagerPackageInfo.FindForAssembly(
                    typeof(SimultriaEndpointCatalog).Assembly);
            if (package == null)
            {
                return false;
            }

            return TryFindDocumentationFile(
                    package.resolvedPath,
                    DeveloperGuideRelativePath,
                    out path) ||
                TryFindDocumentationFile(
                    package.resolvedPath,
                    ReadmeRelativePath,
                    out path);
        }

        private static void OpenDocumentationFile(
            string relativePath,
            string displayName)
        {
            PackageManagerPackageInfo package =
                PackageManagerPackageInfo.FindForAssembly(
                    typeof(SimultriaEndpointCatalog).Assembly);
            if (package == null ||
                !TryFindDocumentationFile(
                    package.resolvedPath,
                    relativePath,
                    out string path))
            {
                ShowMissingDocumentationDialog(displayName);
                return;
            }

            Application.OpenURL(new Uri(path).AbsoluteUri);
        }

        private static void ShowMissingDocumentationDialog(string displayName)
        {
            EditorUtility.DisplayDialog(
                "Simultria API documentation",
                "Could not find the " + displayName +
                " in the installed package.",
                "OK");
        }
    }
}
