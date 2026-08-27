using System;
using System.Linq;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaConnectionSettingsAssetFactoryTests
    {
        private string assetPath;
        private string overridePath;

        [SetUp]
        public void SetUp()
        {
            assetPath = "Assets/SimultriaConnectionSettingsFactoryTests-" +
                Guid.NewGuid().ToString("N") + ".asset";
            overridePath =
                "Assets/SimultriaApiDefinitionOverrideTests-" +
                Guid.NewGuid().ToString("N") + ".asset";
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.DeleteAsset(overridePath);
            AssetDatabase.Refresh();
        }

        [Test]
        public void FactoryCreatesCompleteProjectOwnedSettings()
        {
            bool created = SimultriaConnectionSettingsAssetFactory
                .TryCreateProjectConnectionSettings(
                    assetPath,
                    out ApiConnectionSettings settings,
                    out string error);

            Assert.That(created, Is.True, error);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Environments, Has.Count.EqualTo(4));
            Assert.That(
                settings.Environments.All(environment =>
                    environment.Clients.Count == 1 &&
                    environment.Clients[0].ClientId ==
                        SimultriaClientIds.Primary.Value &&
                    string.IsNullOrEmpty(environment.Clients[0].BaseUrl)),
                Is.True);
            Assert.That(
                SimultriaApiConnectionSettingsAdapter.IsCompatibleSettings(
                    settings,
                    out string message),
                Is.True,
                message);
            Assert.That(
                settings.TryCreateComposition(
                    out ApiComposition composition,
                    out message),
                Is.True,
                message);
            Assert.That(
                composition.GetEnvironmentStatus(
                    SimultriaEnvironmentIds.Development).Availability,
                Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Assert.That(
                assets.OfType<ApiEnvironmentProfile>().Count(),
                Is.EqualTo(4));
        }

        [Test]
        public void MenusExposeOnlyConnectionSettingsAndExplicitContractFork()
        {
            Assert.That(
                SimultriaConnectionSettingsAssetFactory.CreateMenuPath,
                Is.EqualTo(
                    "Assets/Create/Deucarian/Connections/" +
                    "Simultria Connection Settings"));
            Assert.That(
                SimultriaConnectionSettingsAssetFactory
                    .CreateDefinitionOverrideMenuPath,
                Is.EqualTo(
                    "Assets/Create/Deucarian/Connections/Advanced/" +
                    "Simultria API Definition Override"));
        }

        [Test]
        public void FactoryRejectsPathsOutsideProjectAssets()
        {
            Assert.That(
                SimultriaConnectionSettingsAssetFactory
                    .TryCreateProjectConnectionSettings(
                        "Packages/com.example/Settings.asset",
                        out ApiConnectionSettings settings,
                        out string error),
                Is.False);
            Assert.That(settings, Is.Null);
            Assert.That(error, Does.Contain("Assets folder"));
        }

        [Test]
        public void DefinitionOverrideClonesCatalogAndPreservesCompatibility()
        {
            bool created = SimultriaConnectionSettingsAssetFactory
                .TryCreateDefinitionOverrideAsset(
                    overridePath,
                    out ApiServiceDefinition definition,
                    out string error);

            Assert.That(created, Is.True, error);
            Assert.That(definition, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(definition),
                Is.EqualTo(overridePath));
            Assert.That(
                AssetDatabase.GetAssetPath(definition.EndpointCatalog),
                Is.EqualTo(overridePath));
            Assert.That(
                SimultriaApiConnectionSettingsAdapter.IsCompatibleDefinition(
                    definition,
                    out string message),
                Is.True,
                message);
        }
    }
}
