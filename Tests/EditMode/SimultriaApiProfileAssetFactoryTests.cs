using System;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiProfileAssetFactoryTests
    {
        private string assetPath;
        private string catalogOverridePath;

        [SetUp]
        public void SetUp()
        {
            assetPath = "Assets/SimultriaApiProfileFactoryTests-" +
                Guid.NewGuid().ToString("N") + ".asset";
            catalogOverridePath = "Assets/SimultriaApiCatalogOverrideTests-" +
                Guid.NewGuid().ToString("N") + ".asset";
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            if (!string.IsNullOrWhiteSpace(catalogOverridePath))
            {
                AssetDatabase.DeleteAsset(catalogOverridePath);
            }
        }

        [Test]
        public void CreatesProjectProfileWithFourBlankOrderedSubAssets()
        {
            bool created =
                SimultriaApiProfileAssetFactory.TryCreateProjectProfile(
                    assetPath,
                    out SimultriaApiProfile profile,
                    out string error);

            Assert.That(created, Is.True, error);
            Assert.That(profile, Is.Not.Null);
            Assert.That(
                profile.EndpointCatalog,
                Is.SameAs(
                    AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                        SimultriaApiProfileDefaults
                            .DefaultEndpointCatalogAssetPath)));
            Assert.That(profile.Environments.Count, Is.EqualTo(4));

            UnityEngine.Object[] allAssets =
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Assert.That(allAssets.Length, Is.EqualTo(5));
            for (int index = 0;
                index < SimultriaEnvironmentDescriptors.Standard.Count;
                index++)
            {
                ApiEnvironmentDescriptor descriptor =
                    SimultriaEnvironmentDescriptors.Standard[index];
                ApiEnvironmentProfile environment =
                    profile.Environments[index];
                Assert.That(environment.name, Is.EqualTo(descriptor.DisplayName));
                Assert.That(
                    environment.TryGetId(out ApiEnvironmentId environmentId),
                    Is.True);
                Assert.That(environmentId, Is.EqualTo(descriptor.EnvironmentId));
                Assert.That(
                    environment.DisplayName,
                    Is.EqualTo(descriptor.DisplayName));
                Assert.That(
                    AssetDatabase.GetAssetPath(environment),
                    Is.EqualTo(assetPath));
                Assert.That(AssetDatabase.IsSubAsset(environment), Is.True);
                Assert.That(environment.Clients.Count, Is.EqualTo(1));
                Assert.That(
                    environment.Clients[0].ClientId,
                    Is.EqualTo(SimultriaClientIds.Primary.Value));
                Assert.That(environment.Clients[0].BaseUrl, Is.Empty);
                Assert.That(
                    environment.ClassifyConfiguration(out string message),
                    Is.EqualTo(
                        ApiEnvironmentProfileConfigurationState.NotConfigured));
                Assert.That(message, Is.Null);
            }

            ApiComposition composition = profile.CreateComposition();
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                ApiEnvironmentStatus status = composition
                    .GetEnvironmentStatus(descriptor.EnvironmentId);
                Assert.That(
                    status.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(status.Stage, Is.EqualTo(descriptor.Stage));
                Assert.That(status.Message, Does.Contain("not configured"));
            }
        }

        [Test]
        public void CreatesGenericConnectionProfileForNewAuthoring()
        {
            bool created = SimultriaApiProfileAssetFactory
                .TryCreateProjectConnectionProfile(
                    assetPath,
                    out ApiConnectionProfile profile,
                    out string error);

            Assert.That(created, Is.True, error);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Environments, Has.Count.EqualTo(4));
            Assert.That(
                profile.KnownEnvironmentDefinitions,
                Has.Count.EqualTo(4));
            Assert.That(
                profile.EndpointCatalog,
                Is.SameAs(
                    AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                        SimultriaApiProfileDefaults
                            .DefaultEndpointCatalogAssetPath)));
            Assert.That(
                AssetDatabase.LoadAllAssetsAtPath(assetPath),
                Has.Length.EqualTo(5));

            for (int index = 0;
                index < SimultriaEnvironmentDescriptors.Standard.Count;
                index++)
            {
                ApiEnvironmentDescriptor expected =
                    SimultriaEnvironmentDescriptors.Standard[index];
                ApiEnvironmentProfile environment = profile.Environments[index];
                Assert.That(
                    environment.TryGetId(out ApiEnvironmentId environmentId),
                    Is.True);
                Assert.That(environmentId, Is.EqualTo(expected.EnvironmentId));
                Assert.That(
                    environment.TryGetClient(
                        SimultriaClientIds.Primary,
                        out ApiNamedClientDefinition client),
                    Is.True);
                Assert.That(client.BaseUrl, Is.Empty);
            }

            Assert.That(
                SimultriaApiConnectionProfileAdapter.IsCompatibleProfile(
                    profile,
                    out string compatibilityMessage),
                Is.True,
                compatibilityMessage);
        }

        [Test]
        public void NormalCreationMenuKeepsExpertActionsUnderAdvanced()
        {
            Assert.That(
                SimultriaApiProfileAssetFactory.CreateMenuPath,
                Is.EqualTo(
                    "Assets/Create/Deucarian/Simultria/API Profile"));
            Assert.That(
                SimultriaApiProfileAssetFactory.CreateLegacyMenuPath,
                Does.Contain("/Advanced/"));
            Assert.That(
                SimultriaApiProfileAssetFactory.CreateCatalogOverrideMenuPath,
                Does.Contain("/Advanced/"));
        }

        [Test]
        public void RejectsProjectAssetThatWouldShadowPackageResource()
        {
            assetPath = null;
            const string shadowPath =
                "Assets/Resources/Deucarian/Simultria/API/" +
                "SimultriaApiProfile.asset";

            bool created =
                SimultriaApiProfileAssetFactory.TryCreateProjectProfile(
                    shadowPath,
                    out SimultriaApiProfile profile,
                    out string error);

            Assert.That(created, Is.False);
            Assert.That(profile, Is.Null);
            Assert.That(error, Does.Contain("shadow"));
        }

        [Test]
        public void RejectsCatalogOverrideThatWouldShadowPackageResource()
        {
            catalogOverridePath = null;
            const string shadowPath =
                "Assets/Resources/Deucarian/Simultria/API/" +
                "SimultriaApiV2EndpointCatalog.asset";

            bool created = SimultriaApiProfileAssetFactory
                .TryCreateCatalogOverrideAsset(
                    shadowPath,
                    out ApiEndpointCatalog endpointCatalog,
                    out string error);

            Assert.That(created, Is.False);
            Assert.That(endpointCatalog, Is.Null);
            Assert.That(error, Does.Contain("shadow"));
        }

        [Test]
        public void CreatesExplicitProjectOwnedCatalogOverrideWithoutEditingPackage()
        {
            Assert.That(
                SimultriaApiProfileAssetFactory.TryCreateProjectProfile(
                    assetPath,
                    out SimultriaApiProfile profile,
                    out string profileError),
                Is.True,
                profileError);
            ApiEndpointCatalog packageCatalog = profile.EndpointCatalog;
            int packageEndpointCount = packageCatalog.Endpoints.Count;

            bool created = SimultriaApiProfileAssetFactory
                .TryCreateProjectCatalogOverride(
                    profile,
                    catalogOverridePath,
                    out ApiEndpointCatalog catalogOverride,
                    out string error);

            Assert.That(created, Is.True, error);
            Assert.That(catalogOverride, Is.Not.Null);
            Assert.That(catalogOverride, Is.Not.SameAs(packageCatalog));
            Assert.That(profile.EndpointCatalog, Is.SameAs(catalogOverride));
            Assert.That(
                AssetDatabase.GetAssetPath(catalogOverride),
                Is.EqualTo(catalogOverridePath));
            Assert.That(
                catalogOverride.Endpoints.Count,
                Is.EqualTo(packageEndpointCount));
            Assert.That(
                packageCatalog.Endpoints.Count,
                Is.EqualTo(packageEndpointCount));
            Assert.That(
                SimultriaEndpointCatalogEditor
                    .IsCanonicalPackageCatalog(packageCatalog),
                Is.True);
            Assert.That(
                SimultriaEndpointCatalogEditor
                    .IsCanonicalPackageCatalog(catalogOverride),
                Is.False);

            Assert.That(
                SimultriaApiProfileAssetFactory.TryAssignEndpointCatalog(
                    profile,
                    packageCatalog,
                    out string resetError),
                Is.True,
                resetError);
            Assert.That(profile.EndpointCatalog, Is.SameAs(packageCatalog));
        }

        [Test]
        public void RejectsOverrideThatEnablesSensitiveAuthLogging()
        {
            Assert.That(
                SimultriaApiProfileAssetFactory.TryCreateProjectProfile(
                    assetPath,
                    out SimultriaApiProfile profile,
                    out string profileError),
                Is.True,
                profileError);
            Assert.That(
                SimultriaApiProfileAssetFactory
                    .TryCreateProjectCatalogOverride(
                        profile,
                        catalogOverridePath,
                        out ApiEndpointCatalog catalogOverride,
                        out string overrideError),
                Is.True,
                overrideError);
            Assert.That(
                catalogOverride.TryGetEndpoint(
                    SimultriaEndpointIds.Login,
                    out ApiEndpointCatalogEntry login),
                Is.True);
            login.SuppressLogging = false;

            bool assigned = SimultriaApiProfileAssetFactory
                .TryAssignEndpointCatalog(
                    profile,
                    catalogOverride,
                    out string error);

            Assert.That(assigned, Is.False);
            Assert.That(error, Does.Contain("suppress"));
            Assert.Throws<InvalidOperationException>(
                () => profile.CreateComposition());
        }
    }
}
