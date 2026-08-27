using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using Deucarian.Simultria.UnityBuildRouting;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaUnityBuildRoutingServiceTests
    {
        [TestCase("development", "simultria.development")]
        [TestCase("test", "simultria.testing")]
        [TestCase("acceptance", "simultria.acceptance")]
        [TestCase("production", "simultria.production")]
        public async Task ResolvesEveryBackofficeEnvironmentWithoutFallback(
            string backendEnvironment,
            string expectedId)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(
                    ApiEnvironmentId.TryParse(
                        expectedId,
                        out ApiEnvironmentId expectedEnvironment),
                    Is.True);
                fixture.ConfigureEnvironment(
                    expectedEnvironment,
                    "https://target.example.invalid");
                var client = new ApiClientSpy
                {
                    ResponseData = Response(
                        "1.1",
                        "holo_helmet",
                        backendEnvironment)
                };
                var service = new SimultriaUnityBuildRoutingService(
                    client,
                    fixture.Settings.CreateComposition(),
                    SimultriaEnvironmentIds.Development);

                SimultriaUnityBuildRoutingResult result =
                    await service.ResolveAsync("1.1", "holo_helmet");

                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(result.EnvironmentId, Is.EqualTo(expectedEnvironment));
                Assert.That(
                    client.LastEndpoint.Path,
                    Does.EndWith(
                        "/api/v2/unity/builds/versions/1.1/holo_helmet"));
            }
        }

        [Test]
        public async Task RejectsResponseIdentityMismatch()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy
                {
                    ResponseData = Response(
                        "different",
                        "holo_helmet",
                        "development")
                };
                var service = new SimultriaUnityBuildRoutingService(
                    client,
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                SimultriaUnityBuildRoutingResult result =
                    await service.ResolveAsync("1.1", "holo_helmet");

                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo("build_version_mismatch"));
            }
        }

        [Test]
        public async Task RejectsUnknownOrUnconfiguredEnvironment()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy
                {
                    ResponseData = Response(
                        "1.1",
                        "holo_helmet",
                        "unknown")
                };
                var service = new SimultriaUnityBuildRoutingService(
                    client,
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                SimultriaUnityBuildRoutingResult unknown =
                    await service.ResolveAsync("1.1", "holo_helmet");
                client.ResponseData = Response(
                    "1.1",
                    "holo_helmet",
                    "production");
                SimultriaUnityBuildRoutingResult unconfigured =
                    await service.ResolveAsync("1.1", "holo_helmet");

                Assert.That(unknown.ErrorCode, Is.EqualTo("build_environment_unknown"));
                Assert.That(
                    unconfigured.ErrorCode,
                    Is.EqualTo("resolved_environment_unavailable"));
            }
        }

        [TestCase("", "holo_helmet", "build_version_missing")]
        [TestCase("1.1", "", "build_product_missing")]
        public async Task RequiresExplicitBuildIdentity(
            string version,
            string product,
            string expectedError)
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var service = new SimultriaUnityBuildRoutingService(
                    new ApiClientSpy(),
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                SimultriaUnityBuildRoutingResult result =
                    await service.ResolveAsync(version, product);

                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void EvaluatesTransportIndependentResponseWithSameRoutingPolicy()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentId production =
                    SimultriaEnvironmentIds.Production;
                fixture.ConfigureEnvironment(
                    production,
                    "https://target.example.invalid");
                var service = new SimultriaUnityBuildRoutingService(
                    null,
                    fixture.Settings.CreateComposition(),
                    SimultriaEnvironmentIds.Development);

                SimultriaUnityBuildRoutingResult result =
                    service.EvaluateResponse(
                        "1.1",
                        "holo_helmet",
                        new SimultriaUnityBuildVersionDto
                        {
                            Version = "1.1",
                            Product = "holo_helmet",
                            Environment = "production"
                        });

                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(result.EnvironmentId, Is.EqualTo(production));
            }
        }

        private static SimultriaResourceResponse<
            SimultriaUnityBuildVersionDto> Response(
                string version,
                string product,
                string environment)
        {
            return new SimultriaResourceResponse<
                SimultriaUnityBuildVersionDto>
            {
                Data = new SimultriaUnityBuildVersionDto
                {
                    Version = version,
                    Product = product,
                    Environment = environment
                }
            };
        }
    }
}
