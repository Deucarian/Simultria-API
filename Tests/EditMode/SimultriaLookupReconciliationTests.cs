using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaLookupReconciliationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task CentralDiscoveryDoesNotRequireAnyConfiguredRuntimeContext(bool legacyConstructor)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                fixture.ConfigureEnvironment(SimultriaEnvironmentIds.Development, string.Empty);
                ApiComposition composition = fixture.Settings.CreateComposition();
                var client = new ApiClientSpy();
                foreach (ApiEnvironmentDescriptor environment in SimultriaEnvironmentDescriptors.All)
                {
                    Assert.Throws<InvalidOperationException>(() => new SimultriaLookupContext(
                        client, composition, environment.EnvironmentId));
                }

#pragma warning disable CS0618
                var lookup = legacyConstructor
                    ? new SimultriaUnityBuildVersionLookupService(client, composition, SimultriaEnvironmentIds.Local)
                    : new SimultriaUnityBuildVersionLookupService(client);
#pragma warning restore CS0618
                await lookup.GetBuildVersionAsync("1.0.0", "activity_viewer");
                AssertCentralEndpoint(client.LastEndpoint, "activity_viewer");
            }
        }

        [TestCase("activity_viewer")]
        [TestCase("report_viewer")]
        public async Task ExistingContextForwardsOnlyTransportToTheFixedPublicEndpoint(string product)
        {
            using (var fixture = new SimultriaTestComposition())
            using (var cancellation = new CancellationTokenSource())
            {
                fixture.ConfigureEnvironment(SimultriaEnvironmentIds.Development, "https://runtime.example.invalid");
                fixture.Environment.Clients[0].DefaultHeaders.Add(
                    new ApiKeyValuePair { Key = "X-Runtime-Only", Value = "configured-runtime" });
                var entry = fixture.Catalog.Endpoints.Find(candidate =>
                    candidate.EndpointId == SimultriaEndpointIds.UnityBuildVersion.Value);
                entry.RouteTemplate = "not-the-directory/{id}/{product}";
                entry.Authentication = ApiAuthenticationRequirement.Required;
                entry.SuppressLogging = false;
                var client = new ApiClientSpy();
                var context = new SimultriaLookupContext(
                    client, fixture.Settings.CreateComposition(), SimultriaEnvironmentIds.Development);

                // Prove this same context really applies backend defaults to normal services.
                await new SimultriaProjectLookupService(context).GetProjectAsync(42);
                Assert.That(client.LastEndpoint.Path, Is.EqualTo("https://runtime.example.invalid/api/v2/projects/42"));
                Assert.That(client.LastEndpoint.DefaultHeaders["X-Runtime-Only"], Is.EqualTo("configured-runtime"));
                Assert.That(client.LastEndpoint.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));

                var response = new SimultriaResourceResponse<SimultriaUnityBuildVersionDto>
                {
                    Data = new SimultriaUnityBuildVersionDto
                    {
                        Version = "1.0.0", Product = product, Environment = "development"
                    }
                };
                client.ResponseData = response;
#pragma warning disable CS0618
                var lookup = new SimultriaUnityBuildVersionLookupService(context);
                Assert.That(lookup.Composition, Is.SameAs(context.Composition));
                Assert.That(lookup.EnvironmentId, Is.EqualTo(context.EnvironmentId));
#pragma warning restore CS0618
                var result = await lookup.GetBuildVersionAsync("1.0.0", product, cancellation.Token);

                Assert.That(result.Data, Is.SameAs(response));
                Assert.That(client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                AssertCentralEndpoint(client.LastEndpoint, product);
            }
        }

        [Test]
        public void NullConstructorArgumentsAreExplicitlyDisambiguatedAndRejected()
        {
            var clientFailure = Assert.Throws<ArgumentNullException>(() =>
                new SimultriaUnityBuildVersionLookupService((IApiClient)null));
            Assert.That(clientFailure.ParamName, Is.EqualTo("apiClient"));
#pragma warning disable CS0618
            var contextFailure = Assert.Throws<ArgumentNullException>(() =>
                new SimultriaUnityBuildVersionLookupService((SimultriaLookupContext)null));
#pragma warning restore CS0618
            Assert.That(contextFailure.ParamName, Is.EqualTo("context"));
            Assert.That(typeof(SimultriaLookupServiceBase).IsAssignableFrom(
                typeof(SimultriaUnityBuildVersionLookupService)), Is.False);
        }

        [Test]
        public async Task SharedNormalContextKeepsActivityAuthenticationAndCancellation()
        {
            using (var fixture = new SimultriaTestComposition())
            using (var cancellation = new CancellationTokenSource())
            {
                var client = new ApiClientSpy();
                var context = new SimultriaLookupContext(
                    client, fixture.Composition, SimultriaEnvironmentIds.Development);
                SimultriaLookupServiceBase compatibility = new SimultriaActivityLookupService(context);
                Assert.That(compatibility.Composition, Is.SameAs(context.Composition));
                Assert.That(compatibility.EnvironmentStatus, Is.SameAs(context.EnvironmentStatus));

                await ((SimultriaActivityLookupService)compatibility).GetActivitiesAsync(34, cancellation.Token);

                Assert.That(client.LastEndpoint.Path, Is.EqualTo(
                    "https://api.example.invalid/api/v2/projects/models/versions/34/activities"));
                Assert.That(client.LastEndpoint.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));
                Assert.That(client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                Assert.Throws<InvalidOperationException>(() => new SimultriaLookupContext(
                    client, fixture.Composition, SimultriaEnvironmentIds.Local));
                Assert.Throws<ArgumentNullException>(() => new SimultriaProjectLookupService((SimultriaLookupContext)null));
                Assert.Throws<ArgumentNullException>(() => new SimultriaModelLookupService((SimultriaLookupContext)null));
                Assert.Throws<ArgumentNullException>(() => new SimultriaActivityLookupService((SimultriaLookupContext)null));
            }
        }

        [Test]
        public void PackageDoesNotReuseTheUpstreamVersionOrLowerItsDependencyMinima()
        {
            var package = PackageManagerPackageInfo.FindForAssembly(typeof(SimultriaEndpointCatalog).Assembly);
            Assert.That(package, Is.Not.Null);
            JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(package.resolvedPath, "package.json")));
            Assert.That(Version.Parse((string)manifest["version"]), Is.GreaterThanOrEqualTo(new Version(1, 1, 1)));
            Assert.That(Version.Parse((string)manifest["dependencies"]["com.deucarian.editor"]),
                Is.GreaterThanOrEqualTo(new Version(1, 3, 0)));
            Assert.That(Version.Parse((string)manifest["dependencies"]["com.deucarian.session"]),
                Is.GreaterThanOrEqualTo(new Version(1, 0, 7)));
        }

        private static void AssertCentralEndpoint(ApiEndpoint endpoint, string product)
        {
            Assert.That(endpoint.Path, Is.EqualTo(
                "https://buildingvirtualitysuite.com/api/v2/unity/builds/versions/1.0.0/" + product));
            Assert.That(endpoint.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(endpoint.DefaultHeaders, Is.Empty);
            Assert.That(endpoint.SuppressLogging, Is.True);
            Assert.That(endpoint.TimeoutSeconds, Is.EqualTo(30));
        }
    }
}
