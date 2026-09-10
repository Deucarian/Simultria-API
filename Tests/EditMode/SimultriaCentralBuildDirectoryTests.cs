using System;
using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using Deucarian.Simultria.UnityBuildRouting;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaCentralBuildDirectoryTests
    {
        [TestCase("simultria.local")]
        [TestCase("simultria.development")]
        [TestCase("simultria.production")]
        public async Task LegacyDirectorySelectionCannotRedirectLookup(string legacyId)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(ApiEnvironmentId.TryParse(legacyId, out var environment), Is.True);
                fixture.ConfigureEnvironment(environment, "https://other.example.invalid");
                var client = new ApiClientSpy();
#pragma warning disable CS0618
                var lookup = new SimultriaUnityBuildVersionLookupService(
                    client, fixture.Settings.CreateComposition(), environment);
                ApiEndpoint endpoint = SimultriaEndpointCatalog.UnityBuildVersion(
                    fixture.Composition, environment, "1.0", "activity_viewer");
#pragma warning restore CS0618

                await lookup.GetBuildVersionAsync("1.0", "activity_viewer");

                AssertCentralEndpoint(client.LastEndpoint);
                AssertCentralEndpoint(endpoint);
            }
        }

        [Test]
        public async Task EmptyLegacyDirectoryAndUnconfiguredProductionDoNotBlockDiscovery()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy { ResponseData = Response("development") };
#pragma warning disable CS0618
                var router = new SimultriaUnityBuildRoutingService(
                    client, fixture.Composition, default(ApiEnvironmentId));
                var lookup = new SimultriaUnityBuildVersionLookupService(
                    client, null, default(ApiEnvironmentId));
#pragma warning restore CS0618

                await lookup.GetBuildVersionAsync("1.0", "activity_viewer");
                AssertCentralEndpoint(client.LastEndpoint);
                var result = await router.ResolveAsync("1.0", "activity_viewer");

                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(result.EnvironmentId, Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(result.IsVersionMissing, Is.False);
                AssertCentralEndpoint(client.LastEndpoint);
            }
        }

        [Test]
        public async Task CentralLookupDoesNotApplyTargetCatalogAuthenticationOrRouteOverrides()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var entry = fixture.Catalog.Endpoints.Find(candidate =>
                    candidate.EndpointId == SimultriaEndpointIds.UnityBuildVersion.Value);
                entry.RouteTemplate = "redirected/{id}/{product}";
                entry.Authentication = ApiAuthenticationRequirement.Required;
                entry.SuppressLogging = false;
                var client = new ApiClientSpy { ResponseData = Response("development") };
                var result = await new SimultriaUnityBuildRoutingService(
                    client, fixture.Settings.CreateComposition()).ResolveAsync("1.0", "activity_viewer");

                Assert.That(result.Succeeded, Is.True, result.Message);
                AssertCentralEndpoint(client.LastEndpoint);
            }
        }

        [Test]
        public async Task NormalLookupStillUsesExplicitConfiguredBackend()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy();
                await new SimultriaProjectLookupService(
                    client, fixture.Composition, SimultriaEnvironmentIds.Development)
                    .GetProjectAsync(42);

                Assert.That(client.LastEndpoint.Path,
                    Is.EqualTo("https://api.example.invalid/api/v2/projects/42"));
                Assert.That(client.LastEndpoint.Authentication,
                    Is.EqualTo(ApiAuthenticationRequirement.Required));
                Assert.Throws<InvalidOperationException>(() => new SimultriaProjectLookupService(
                    client, fixture.Composition, SimultriaEnvironmentIds.Local));
            }
        }

        private static void AssertCentralEndpoint(ApiEndpoint endpoint)
        {
            Assert.That(endpoint.Path, Is.EqualTo(
                "https://buildingvirtualitysuite.com/api/v2/unity/builds/versions/1.0/activity_viewer"));
            Assert.That(endpoint.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(endpoint.SuppressLogging, Is.True);
            Assert.That(endpoint.DefaultHeaders, Is.Empty);
            Assert.That(endpoint.TimeoutSeconds, Is.EqualTo(30));
        }

        private static SimultriaResourceResponse<SimultriaUnityBuildVersionDto> Response(
            string environment)
        {
            return new SimultriaResourceResponse<SimultriaUnityBuildVersionDto>
            {
                Data = new SimultriaUnityBuildVersionDto
                {
                    Version = "1.0", Product = "activity_viewer", Environment = environment
                }
            };
        }
    }
}
