using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using Deucarian.Simultria.UnityBuildRouting;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaUnityBuildLookupEnvironmentTests
    {
        [Test]
        public void StableSelectionDefaultsToProductionAndHasOnlyTwoSupportedValues()
        {
            Assert.That((int)SimultriaUnityBuildLookupEnvironment.Production, Is.Zero);
            Assert.That((int)SimultriaUnityBuildLookupEnvironment.Development, Is.EqualTo(1));
            Assert.That(Enum.GetValues(typeof(SimultriaUnityBuildLookupEnvironment)).Length, Is.EqualTo(2));
            Assert.That(default(SimultriaUnityBuildLookupEnvironment),
                Is.EqualTo(SimultriaUnityBuildLookupEnvironment.Production));
        }

        [TestCase(SimultriaUnityBuildLookupEnvironment.Production, "https://buildingvirtualitysuite.com")]
        [TestCase(SimultriaUnityBuildLookupEnvironment.Development, "https://backend.dev-buildingvirtuality.com")]
        public void SupportedSelectionUsesOnlyItsApiOwnedHost(
            SimultriaUnityBuildLookupEnvironment selection, string expectedBaseUrl)
        {
            Assert.That(SimultriaUnityBuildDirectory.TryGetBaseUrl(selection, out string baseUrl), Is.True);
            Assert.That(baseUrl, Is.EqualTo(expectedBaseUrl));
            AssertPublicEndpoint(SimultriaEndpointCatalog.UnityBuildVersion("AV-1.0.0", "activity_viewer", selection),
                expectedBaseUrl, "AV-1.0.0", "activity_viewer");
            AssertPublicEndpoint(SimultriaEndpointCatalog.UnityBuildVersion("RV-1.0.0", "report_viewer", selection),
                expectedBaseUrl, "RV-1.0.0", "report_viewer");
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public async Task UnsupportedSelectionFailsClosedBeforeTransport(int value)
        {
            var selection = (SimultriaUnityBuildLookupEnvironment)value;
            var client = new ApiClientSpy();
            Assert.That(SimultriaUnityBuildDirectory.TryGetBaseUrl(selection, out string baseUrl), Is.False);
            Assert.That(baseUrl, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SimultriaEndpointCatalog.UnityBuildVersion("AV-1.0.0", "activity_viewer", selection));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SimultriaUnityBuildVersionLookupService(client, selection));

            using (var fixture = new SimultriaTestComposition())
            {
                var result = await new SimultriaUnityBuildRoutingService(client, selection, fixture.Composition)
                    .ResolveAsync("AV-1.0.0", "activity_viewer");
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo("build_lookup_environment_invalid"));
                Assert.That(result.IsVersionMissing, Is.False);
                Assert.That(result.EnvironmentId.IsEmpty, Is.True);
            }

            Assert.That(client.SendCount, Is.Zero);
            Assert.That(client.LastEndpoint, Is.Null);
        }

        [TestCase(SimultriaUnityBuildLookupEnvironment.Production, "https://buildingvirtualitysuite.com")]
        [TestCase(SimultriaUnityBuildLookupEnvironment.Development, "https://backend.dev-buildingvirtuality.com")]
        public async Task ExplicitLookupNeedsNoRuntimeProfileAndPreservesCancellation(
            SimultriaUnityBuildLookupEnvironment selection, string expectedBaseUrl)
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var response = Response("development");
                var client = new ApiClientSpy { ResponseData = response };
                var result = await new SimultriaUnityBuildVersionLookupService(client, selection)
                    .GetBuildVersionAsync("AV-1.0.0", "activity_viewer", cancellation.Token);

                Assert.That(result.Data, Is.SameAs(response));
                Assert.That(client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(client.SendCount, Is.EqualTo(1));
                AssertPublicEndpoint(client.LastEndpoint, expectedBaseUrl, "AV-1.0.0", "activity_viewer");
            }
        }

        [TestCase(SimultriaUnityBuildLookupEnvironment.Development, "production",
            "simultria.production", "https://backend.dev-buildingvirtuality.com")]
        [TestCase(SimultriaUnityBuildLookupEnvironment.Production, "development",
            "simultria.development", "https://buildingvirtualitysuite.com")]
        public async Task LookupSelectionDoesNotChooseRuntimeOrInheritItsRequestPolicy(
            SimultriaUnityBuildLookupEnvironment selection, string returnedEnvironment,
            string expectedRuntimeId, string expectedBaseUrl)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                fixture.ConfigureEnvironment(SimultriaEnvironmentIds.Production, "https://runtime-prod.example.invalid");
                fixture.ConfigureEnvironment(SimultriaEnvironmentIds.Development, "https://runtime-dev.example.invalid");
                fixture.Environment.Clients[0].DefaultHeaders.Add(
                    new ApiKeyValuePair { Key = "X-Runtime-Only", Value = "private-runtime-header" });
                var entry = fixture.Catalog.Endpoints.Find(candidate =>
                    candidate.EndpointId == SimultriaEndpointIds.UnityBuildVersion.Value);
                entry.RouteTemplate = "custom/{id}/{product}";
                entry.Authentication = ApiAuthenticationRequirement.Required;
                entry.SuppressLogging = false;
                var client = new ApiClientSpy { ResponseData = Response(returnedEnvironment) };
                var result = await new SimultriaUnityBuildRoutingService(
                    client, selection, fixture.Settings.CreateComposition())
                    .ResolveAsync("AV-1.0.0", "activity_viewer");

                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(result.EnvironmentId.Value, Is.EqualTo(expectedRuntimeId));
                Assert.That(result.IsVersionMissing, Is.False);
                Assert.That(client.SendCount, Is.EqualTo(1));
                AssertPublicEndpoint(client.LastEndpoint, expectedBaseUrl, "AV-1.0.0", "activity_viewer");
            }
        }

        [Test]
        public async Task DevelopmentLookupDoesNotSubstituteItsRuntimeForUnconfiguredProduction()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy { ResponseData = Response("production") };
                var result = await new SimultriaUnityBuildRoutingService(
                    client, SimultriaUnityBuildLookupEnvironment.Development, fixture.Composition)
                    .ResolveAsync("AV-1.0.0", "activity_viewer");
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo("resolved_environment_unavailable"));
                Assert.That(result.IsVersionMissing, Is.False);
                Assert.That(client.SendCount, Is.EqualTo(1));
            }
        }

        [TestCase(SimultriaUnityBuildLookupEnvironment.Production)]
        [TestCase(SimultriaUnityBuildLookupEnvironment.Development)]
        public void CancellationPropagatesWithoutTryingAnotherDirectory(SimultriaUnityBuildLookupEnvironment selection)
        {
            using (var fixture = new SimultriaTestComposition())
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                var client = new ApiClientSpy { ThrownException = new OperationCanceledException(cancellation.Token) };
                Assert.CatchAsync<OperationCanceledException>(async () =>
                    await new SimultriaUnityBuildRoutingService(client, selection, fixture.Composition)
                        .ResolveAsync("AV-1.0.0", "activity_viewer", cancellation.Token));
                Assert.That(client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(client.SendCount, Is.EqualTo(1));
            }
        }

        private static void AssertPublicEndpoint(ApiEndpoint endpoint, string baseUrl, string version, string product)
        {
            Assert.That(endpoint.Path, Is.EqualTo(baseUrl + "/api/v2/unity/builds/versions/" + version + "/" + product));
            Assert.That(endpoint.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(endpoint.DefaultHeaders, Is.Empty);
            Assert.That(endpoint.SuppressLogging, Is.True);
            Assert.That(endpoint.TimeoutSeconds, Is.EqualTo(30));
        }

        private static SimultriaResourceResponse<SimultriaUnityBuildVersionDto> Response(string environment)
        {
            return new SimultriaResourceResponse<SimultriaUnityBuildVersionDto>
            {
                Data = new SimultriaUnityBuildVersionDto
                {
                    Version = "AV-1.0.0", Product = "activity_viewer", Environment = environment
                }
            };
        }
    }
}
