using System;
using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaUnityBuildVersionLookupTests
    {
        [Test]
        public async Task LookupUsesPublicCredentialFreeBuildDirectoryRoute()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var client = new ApiClientSpy();
                var service = new SimultriaUnityBuildVersionLookupService(
                    client,
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                await service.GetBuildVersionAsync(
                    "build 42",
                    "report_viewer");

                Assert.That(
                    client.LastEndpoint.Path,
                    Does.EndWith(
                        "/api/v2/unity/builds/versions/" +
                        "build%2042/report_viewer"));
                Assert.That(
                    client.LastEndpoint.Authentication,
                    Is.EqualTo(ApiAuthenticationRequirement.Disabled));
                Assert.That(client.LastEndpoint.SuppressLogging, Is.True);
            }
        }

        [TestCase("development", "simultria.development")]
        [TestCase("test", "simultria.testing")]
        [TestCase("testing", "simultria.testing")]
        [TestCase("accept", "simultria.acceptance")]
        [TestCase("acceptance", "simultria.acceptance")]
        [TestCase("production", "simultria.production")]
        public void MapsDocumentedBuildEnvironmentNames(
            string backendName,
            string expectedId)
        {
            bool mapped = SimultriaBuildEnvironmentNameMapper.TryMap(
                backendName,
                out ApiEnvironmentId environmentId,
                out string error);

            Assert.That(mapped, Is.True, error);
            Assert.That(environmentId.Value, Is.EqualTo(expectedId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("deprecated")]
        [TestCase("production-like")]
        public void MissingOrUnknownEnvironmentFailsClosed(string backendName)
        {
            bool mapped = SimultriaBuildEnvironmentNameMapper.TryMap(
                backendName,
                out ApiEnvironmentId environmentId,
                out string error);

            Assert.That(mapped, Is.False);
            Assert.That(environmentId.IsEmpty, Is.True);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void DeserializesDocumentedSnakeCaseBuildResponse()
        {
            const string json = "{\"data\":{" +
                "\"version\":\"529c5c97\"," +
                "\"product\":\"design_and_sales\"," +
                "\"environment\":\"testing\"," +
                "\"config\":{\"backend_url\":\"ignored.example\"}}}";

            var response = JsonConvert.DeserializeObject<
                SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>(
                    json);

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Data.Version, Is.EqualTo("529c5c97"));
            Assert.That(response.Data.Product, Is.EqualTo("design_and_sales"));
            Assert.That(response.Data.Environment, Is.EqualTo("testing"));
        }

        [Test]
        public void LookupRejectsMissingVersionOrProductBeforeTransport()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var service = new SimultriaUnityBuildVersionLookupService(
                    new ApiClientSpy(),
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                Assert.Throws<ArgumentException>(() =>
                    service.GetBuildVersionAsync("", "report_viewer"));
                Assert.Throws<ArgumentException>(() =>
                    service.GetBuildVersionAsync("build-42", ""));
            }
        }
    }
}
