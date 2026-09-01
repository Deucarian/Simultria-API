using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiConnectionSettingsTests
    {
        [Test]
        public void CanonicalEnvironmentDescriptorsAreStableAndHostFree()
        {
            AssertDescriptor(
                SimultriaEnvironmentDescriptors.Local,
                SimultriaEnvironmentIds.Local,
                ApiEnvironmentStage.Custom,
                "Local");
            AssertDescriptor(
                SimultriaEnvironmentDescriptors.Development,
                SimultriaEnvironmentIds.Development,
                ApiEnvironmentStage.Development,
                "Development");
            AssertDescriptor(
                SimultriaEnvironmentDescriptors.Testing,
                SimultriaEnvironmentIds.Testing,
                ApiEnvironmentStage.Testing,
                "Testing");
            AssertDescriptor(
                SimultriaEnvironmentDescriptors.Acceptance,
                SimultriaEnvironmentIds.Acceptance,
                ApiEnvironmentStage.Acceptance,
                "Acceptance");
            AssertDescriptor(
                SimultriaEnvironmentDescriptors.Production,
                SimultriaEnvironmentIds.Production,
                ApiEnvironmentStage.Production,
                "Production");
            Assert.That(
                SimultriaEnvironmentDescriptors.Standard,
                Has.Count.EqualTo(4));
            CollectionAssert.AreEqual(
                new[]
                {
                    SimultriaEnvironmentDescriptors.Local,
                    SimultriaEnvironmentDescriptors.Development,
                    SimultriaEnvironmentDescriptors.Testing,
                    SimultriaEnvironmentDescriptors.Acceptance,
                    SimultriaEnvironmentDescriptors.Production
                },
                SimultriaEnvironmentDescriptors.All);
            Assert.That(
                typeof(ApiEnvironmentDescriptor).GetProperty("BaseUrl"),
                Is.Null);
        }

        [Test]
        public void PackageDefinitionIsCredentialFreeAndMatchesGeneratedCatalog()
        {
            ApiServiceDefinition definition =
                SimultriaApiDefinitionDefaults.LoadServiceDefinition();

            Assert.That(definition, Is.Not.Null);
            Assert.That(
                SimultriaApiConnectionSettingsAdapter.IsCompatibleDefinition(
                    definition,
                    out string message),
                Is.True,
                message);
            Assert.That(definition.ServiceId,
                Is.EqualTo(SimultriaServiceIds.ApiV2.Value));
            Assert.That(definition.EndpointCatalog.CatalogId,
                Is.EqualTo(SimultriaCatalogIds.ApiV2.Value));
            Assert.That(definition.SourceVersion, Is.Not.Empty);
            Assert.That(definition.SourceFingerprint,
                Does.StartWith("sha256:"));
            Assert.That(
                definition.TryGetRequiredClientIds(
                    out var clients,
                    out message),
                Is.True,
                message);
            CollectionAssert.Contains(clients, SimultriaClientIds.Primary);
            Assert.That(
                typeof(ApiServiceDefinition).GetProperty("BaseUrl"),
                Is.Null);
        }

        [Test]
        public void ExplicitSettingsComposeConfiguredAndUnconfiguredEnvironments()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(
                    SimultriaApiConnectionSettingsAdapter.TryCreateComposition(
                        fixture.Settings,
                        out ApiComposition composition,
                        out string message),
                    Is.True,
                    message);
                Assert.That(
                    composition.GetEnvironmentStatus(
                        SimultriaEnvironmentIds.Development).Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Configured));
                Assert.That(
                    composition.GetEnvironmentStatus(
                        SimultriaEnvironmentIds.Testing).Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(
                    composition.GetEnvironmentStatus(
                        default(ApiEnvironmentId)).Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unknown));
            }
        }

        [Test]
        public void MissingSettingsAreAHardConfigurationError()
        {
            Assert.That(
                SimultriaApiConnectionSettingsAdapter.TryCreateComposition(
                    null,
                    out ApiComposition composition,
                    out string message),
                Is.False);
            Assert.That(composition, Is.Null);
            Assert.That(message, Does.Contain("Assign"));
        }

        [Test]
        public void InvalidOrPartialHostsFailClosed()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                fixture.Environment.Clients[0].BaseUrl =
                    "not-an-absolute-http-url";
                Assert.That(
                    SimultriaApiConnectionSettingsAdapter.TryCreateComposition(
                        fixture.Settings,
                        out _,
                        out string message),
                    Is.False);
                Assert.That(message, Does.Contain("HTTP(S)"));
            }
        }

        [Test]
        public void WrongServiceDefinitionIsRejected()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                fixture.Definition.ServiceId = "another.api";
                Assert.That(
                    SimultriaApiConnectionSettingsAdapter.IsCompatibleSettings(
                        fixture.Settings,
                        out string message),
                    Is.False);
                Assert.That(message,
                    Does.Contain(SimultriaServiceIds.ApiV2.Value));
            }
        }

        [Test]
        public void GeneratedCatalogContainsRequiredSafeAuthenticationEndpoints()
        {
            ApiEndpointCatalog catalog =
                SimultriaApiDefinitionDefaults.LoadEndpointCatalog();
            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGetEndpoint(
                    SimultriaEndpointIds.Login,
                    out ApiEndpointCatalogEntry login),
                Is.True);
            Assert.That(login.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(login.SuppressLogging, Is.True);
            Assert.That(
                catalog.TryGetEndpoint(
                    SimultriaEndpointIds.ValidateAuthentication,
                    out ApiEndpointCatalogEntry validation),
                Is.True);
            Assert.That(validation.SuppressLogging, Is.True);
        }

        [Test]
        public void PackageFilesRejectRemovedConnectionArtifacts()
        {
            string packageRoot = Path.GetFullPath(
                "Packages/com.deucarian.simultria-api");
            string removedType = "Simultria" + "ApiProfile";
            string removedField = "apiProfile" + "Reference";
            string removedScriptGuid =
                "98c59614" + "849544b49d34f059afc91fb5";
            var forbidden = new[]
            {
                removedType,
                removedField,
                removedScriptGuid
            };

            foreach (string file in Directory.GetFiles(
                         packageRoot,
                         "*",
                         SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.EndsWith("CHANGELOG.md",
                        StringComparison.OrdinalIgnoreCase) ||
                    normalized.Contains("/.git/"))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (extension != ".cs" && extension != ".asset" &&
                    extension != ".prefab" && extension != ".unity" &&
                    extension != ".json" && extension != ".md")
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                foreach (string value in forbidden)
                {
                    Assert.That(
                        text.Contains(value),
                        Is.False,
                        file + " contains removed connection data.");
                }
            }
        }

        private static void AssertDescriptor(
            ApiEnvironmentDescriptor descriptor,
            ApiEnvironmentId environmentId,
            ApiEnvironmentStage stage,
            string displayName)
        {
            Assert.That(descriptor.EnvironmentId, Is.EqualTo(environmentId));
            Assert.That(descriptor.Stage, Is.EqualTo(stage));
            Assert.That(descriptor.DisplayName, Is.EqualTo(displayName));
        }
    }
}
