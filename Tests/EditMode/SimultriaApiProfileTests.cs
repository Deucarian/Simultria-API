using System;
using System.Collections.Generic;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiProfileTests
    {
        [Test]
        public void StandardDescriptorsAreStableAndCanonicallyOrdered()
        {
            var descriptors = SimultriaEnvironmentDescriptors.Standard;

            Assert.That(descriptors.Count, Is.EqualTo(4));
            AssertDescriptor(
                descriptors[0],
                SimultriaEnvironmentIds.Development,
                ApiEnvironmentStage.Development,
                "Development");
            AssertDescriptor(
                descriptors[1],
                SimultriaEnvironmentIds.Testing,
                ApiEnvironmentStage.Testing,
                "Testing");
            AssertDescriptor(
                descriptors[2],
                SimultriaEnvironmentIds.Acceptance,
                ApiEnvironmentStage.Acceptance,
                "Acceptance");
            AssertDescriptor(
                descriptors[3],
                SimultriaEnvironmentIds.Production,
                ApiEnvironmentStage.Production,
                "Production");
        }

        [Test]
        public void DevelopmentIdResolvesDocumentedEnvironmentWithoutSelectionState()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentStatus status = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Development);

                Assert.That(status.IsResolved, Is.True);
                Assert.That(status.EnvironmentId,
                    Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(status.DisplayName,
                    Is.EqualTo("Simultria Development"));
                Assert.That(status.Message, Is.Null);
            }
        }

        [Test]
        public void ProfileReturnsSanitizedStatusWithoutApiOrigin()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                bool resolved = fixture.Profile.TryGetEnvironmentStatus(
                    SimultriaEnvironmentIds.Development,
                    out ApiEnvironmentStatus status,
                    out string message);

                Assert.That(resolved, Is.True);
                Assert.That(status.GetType().GetProperty("BaseUrl"), Is.Null);
                Assert.That(message, Is.Null);
            }
        }

        [Test]
        public void PackageDefaultProfileContainsNoDeploymentUrl()
        {
            SimultriaApiProfile profile = SimultriaApiProfileDefaults.Load();

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Environments.Count, Is.EqualTo(4));
            foreach (ApiEnvironmentProfile environment in profile.Environments)
            {
                Assert.That(environment.Clients.Count, Is.EqualTo(1));
                Assert.That(environment.Clients[0].BaseUrl, Is.Empty);
            }
            ApiComposition composition = profile.CreateComposition();
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                ApiEnvironmentStatus status = composition.GetEnvironmentStatus(
                    descriptor.EnvironmentId);
                Assert.That(
                    status.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(status.Stage, Is.EqualTo(descriptor.Stage));
                Assert.That(
                    composition.TryResolveEndpoint(
                        descriptor.EnvironmentId,
                        SimultriaEndpointIds.Login,
                        out _,
                        out string message),
                    Is.False);
                Assert.That(message, Does.Contain("not configured"));
            }
        }

        [Test]
        public void PackageCatalogMatchesGeneratedContractManifest()
        {
            ApiEndpointCatalog catalog =
                SimultriaApiProfileDefaults.LoadEndpointCatalog();
            Assert.That(
                SimultriaContractUpdateService.TryLoadCurrentManifest(
                    out SimultriaContractManifestDocument manifest,
                    out string manifestError),
                Is.True,
                manifestError);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.IsValid(out string validationMessage),
                Is.True,
                validationMessage);
            Assert.That(
                catalog.Endpoints,
                Has.Count.EqualTo(manifest.catalog.operationCount));
            Assert.That(manifest.coverage.snapshotCoverageComplete, Is.True);

            var endpointIds = new HashSet<string>(StringComparer.Ordinal);
            int unauthenticatedCount = 0;
            int derivedCount = 0;
            foreach (ApiEndpointCatalogEntry endpoint in catalog.Endpoints)
            {
                Assert.That(endpointIds.Add(endpoint.EndpointId), Is.True);
                Assert.That(
                    endpoint.ClientId,
                    Is.EqualTo(SimultriaClientIds.Primary.Value));
                Assert.That(
                    Uri.TryCreate(
                        endpoint.RouteTemplate,
                        UriKind.Absolute,
                        out _),
                    Is.False);
                if (endpoint.Authentication ==
                    ApiAuthenticationRequirement.Disabled)
                {
                    unauthenticatedCount++;
                }

                if (endpoint.EndpointId.StartsWith(
                    "simultria.generated.",
                    StringComparison.Ordinal))
                {
                    derivedCount++;
                    Assert.That(
                        endpoint.SuppressLogging,
                        Is.True,
                        endpoint.EndpointId);
                }
            }

            Assert.That(
                unauthenticatedCount,
                Is.EqualTo(manifest.catalog.unauthenticatedOperationCount));
            Assert.That(
                derivedCount,
                Is.EqualTo(manifest.catalog.generatedOperationCount));
            Assert.That(
                catalog.TryGetEndpoint(
                    SimultriaEndpointIds.Login,
                    out ApiEndpointCatalogEntry login),
                Is.True);
            Assert.That(login.Method, Is.EqualTo(Deucarian.API.HttpMethod.POST));
            Assert.That(
                login.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(login.SuppressLogging, Is.True);
            Assert.That(
                catalog.TryGetEndpoint(
                    SimultriaEndpointIds.UnityBuildVersion,
                    out ApiEndpointCatalogEntry unityBuildVersion),
                Is.True);
            Assert.That(
                unityBuildVersion.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(unityBuildVersion.SuppressLogging, Is.True);
            Assert.That(
                catalog.TryGetEndpoint(
                    new ApiEndpointId(
                        "simultria.generated.get.api.v2.companies.demo-model"),
                    out ApiEndpointCatalogEntry publicDemoModel),
                Is.True);
            Assert.That(
                publicDemoModel.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(
                catalog.TryGetEndpoint(
                    new ApiEndpointId(
                        "simultria.generated.post.api.v2.projects." +
                        "by-project_id.multiplayer.tokens"),
                    out ApiEndpointCatalogEntry multiplayerToken),
                Is.True);
            Assert.That(multiplayerToken.SuppressLogging, Is.True);
        }

        [Test]
        public void FutureEnvironmentIdsDoNotInventDeploymentUrls()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentStatus testing = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Testing);
                ApiEnvironmentStatus acceptance = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Acceptance);
                ApiEnvironmentStatus production = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Production);

                Assert.That(
                    testing.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(
                    acceptance.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(
                    production.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(testing.Message, Does.Contain("not configured"));
                Assert.That(acceptance.GetType().GetProperty("BaseUrl"), Is.Null);
                Assert.That(production.GetType().GetProperty("BaseUrl"), Is.Null);
            }
        }

        [Test]
        public void BlankKnownProfileIsUnconfiguredWithoutBlockingDevelopment()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentProfile testing = CreateEnvironment(
                    SimultriaEnvironmentIds.Testing,
                    "Testing",
                    string.Empty);
                SimultriaApiProfile profile = SimultriaApiProfile.CreateTransient(
                    new[] { fixture.Environment, testing },
                    fixture.Catalog);
                try
                {
                    ApiComposition composition = profile.CreateComposition();

                    Assert.That(
                        composition.GetEnvironmentStatus(
                            SimultriaEnvironmentIds.Development).Availability,
                        Is.EqualTo(ApiEnvironmentAvailability.Configured));
                    ApiEnvironmentStatus status = composition
                        .GetEnvironmentStatus(SimultriaEnvironmentIds.Testing);
                    Assert.That(
                        status.Availability,
                        Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                    Assert.That(status.Message, Does.Contain("not configured"));
                    Assert.That(
                        composition.TryResolveClient(
                            SimultriaEnvironmentIds.Testing,
                            SimultriaClientIds.Primary,
                            out _,
                            out string message),
                        Is.False);
                    Assert.That(message, Does.Contain("not configured"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                    UnityEngine.Object.DestroyImmediate(testing);
                }
            }
        }

        [Test]
        public void NonEmptyInvalidHostFailsClosed()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentProfile testing = CreateEnvironment(
                    SimultriaEnvironmentIds.Testing,
                    "Testing",
                    "not-an-absolute-http-url");
                SimultriaApiProfile profile = SimultriaApiProfile.CreateTransient(
                    new[] { fixture.Environment, testing },
                    fixture.Catalog);
                try
                {
                    Assert.Throws<ArgumentException>(
                        () => profile.CreateComposition());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                    UnityEngine.Object.DestroyImmediate(testing);
                }
            }
        }

        [Test]
        public void GenericDefaultProfileFailsBeforeEndpointResolution()
        {
            ApiEndpointCatalog catalog =
                SimultriaApiProfileDefaults.LoadEndpointCatalog();
            var environments = new List<ApiEnvironmentProfile>();
            var descriptors = new List<ApiEnvironmentDescriptor>();
            foreach (ApiEnvironmentStage stage in ApiEnvironmentStages.Standard)
            {
                string id = stage.ToString().ToLowerInvariant();
                descriptors.Add(
                    new ApiEnvironmentDescriptor(
                        new ApiEnvironmentId(id),
                        stage,
                        stage.ToString()));
                environments.Add(
                    CreateEnvironment(
                        new ApiEnvironmentId(id),
                        stage.ToString(),
                        string.Empty,
                        "primary"));
            }

            ApiConnectionProfile profile =
                ApiConnectionProfile.CreateTransient(
                    environments,
                    catalog,
                    descriptors);
            try
            {
                Assert.That(
                    SimultriaApiConnectionProfileAdapter.IsCompatibleProfile(
                        profile,
                        out string message),
                    Is.False);
                Assert.That(message, Does.Contain("simultria.development"));
                Assert.That(
                    SimultriaApiConnectionProfileAdapter.TryCreateComposition(
                        profile,
                        out ApiComposition composition,
                        out message),
                    Is.False);
                Assert.That(composition, Is.Null);
                Assert.That(message, Does.Contain("simultria.development"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    UnityEngine.Object.DestroyImmediate(environment);
                }
            }
        }

        [Test]
        public void SimultriaGenericProfileRequiresSimultriaPrimaryClient()
        {
            ApiEndpointCatalog catalog =
                SimultriaApiProfileDefaults.LoadEndpointCatalog();
            var environments = new List<ApiEnvironmentProfile>();
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                environments.Add(
                    CreateEnvironment(
                        descriptor.EnvironmentId,
                        descriptor.DisplayName,
                        string.Empty,
                        "primary"));
            }

            ApiConnectionProfile profile =
                ApiConnectionProfile.CreateTransient(
                    environments,
                    catalog,
                    SimultriaEnvironmentDescriptors.Standard);
            try
            {
                Assert.That(
                    SimultriaApiConnectionProfileAdapter.IsCompatibleProfile(
                        profile,
                        out string message),
                    Is.False);
                Assert.That(message, Does.Contain("simultria.primary"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    UnityEngine.Object.DestroyImmediate(environment);
                }
            }
        }

        private static ApiEnvironmentProfile CreateEnvironment(
            ApiEnvironmentId environmentId,
            string displayName,
            string baseUrl,
            string clientId = null)
        {
            ApiEnvironmentProfile environment =
                ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
            environment.EnvironmentId = environmentId.Value;
            environment.DisplayName = displayName;
            environment.Clients.Add(
                new ApiNamedClientDefinition
                {
                    ClientId = clientId ??
                        SimultriaClientIds.Primary.Value,
                    BaseUrl = baseUrl
                });
            return environment;
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
