using System;
using System.IO;
using Deucarian.Simultria.API.Editor;
using Deucarian.Simultria.API.Endpoints;
using NUnit.Framework;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiDocumentationMenuTests
    {
        [Test]
        public void InstalledPackageContainsDeveloperDocumentation()
        {
            PackageManagerPackageInfo package =
                PackageManagerPackageInfo.FindForAssembly(
                    typeof(SimultriaEndpointCatalog).Assembly);

            Assert.That(package, Is.Not.Null);
            Assert.That(
                SimultriaApiDocumentationMenu.TryFindDocumentation(
                    out string installedDocumentation),
                Is.True);
            Assert.That(
                Path.GetFileName(installedDocumentation),
                Is.EqualTo("index.md"));
            Assert.That(
                SimultriaApiDocumentationMenu.TryFindDocumentationFile(
                    package.resolvedPath,
                    "Documentation~/index.md",
                    out string developerGuide),
                Is.True);
            Assert.That(File.ReadAllText(developerGuide),
                Does.Contain("SimultriaActivityLookupService"));
            Assert.That(
                SimultriaApiDocumentationMenu.TryFindDocumentationFile(
                    package.resolvedPath,
                    "Documentation~/PUBLIC_API.md",
                    out string publicApi),
                Is.True);
            Assert.That(File.ReadAllText(publicApi),
                Does.Contain("SimultriaEndpointCatalog"));
            Assert.That(
                SimultriaApiDocumentationMenu.TryFindDocumentationFile(
                    package.resolvedPath,
                    "Documentation~/Generated/API-Endpoints.md",
                    out _),
                Is.True);
        }

        [Test]
        public void DocumentationLookupRejectsAPathOutsideThePackage()
        {
            string packageRoot = Path.Combine(
                Path.GetTempPath(),
                "simultria-api-docs-" + Guid.NewGuid().ToString("N"));
            string outsidePath = packageRoot + "-outside.md";
            Directory.CreateDirectory(packageRoot);
            File.WriteAllText(outsidePath, "outside");

            try
            {
                Assert.That(
                    SimultriaApiDocumentationMenu.TryFindDocumentationFile(
                        packageRoot,
                        "../" + Path.GetFileName(outsidePath),
                        out _),
                    Is.False);
            }
            finally
            {
                File.Delete(outsidePath);
                Directory.Delete(packageRoot);
            }
        }
    }
}
