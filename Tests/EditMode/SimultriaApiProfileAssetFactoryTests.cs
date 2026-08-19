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

        [SetUp]
        public void SetUp()
        {
            assetPath = "Assets/SimultriaApiProfileFactoryTests-" +
                Guid.NewGuid().ToString("N") + ".asset";
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
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
    }
}
