using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Services;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaLookupServiceTests
    {
        private ApiClientSpy apiClient;
        private SimultriaTestComposition fixture;

        [SetUp]
        public void SetUp()
        {
            apiClient = new ApiClientSpy();
            fixture = new SimultriaTestComposition();
        }

        [TearDown]
        public void TearDown()
        {
            fixture.Dispose();
        }

        [Test]
        public async Task ProjectLookupSendsTypedAuthenticatedEndpoint()
        {
            var service = new SimultriaProjectLookupService(
                apiClient,
                fixture.Composition,
                SimultriaEnvironmentIds.Development);

            await service.GetProjectAsync(12);

            Assert.That(apiClient.LastEndpoint.Path,
                Does.EndWith("/api/v2/projects/12"));
            Assert.That(apiClient.LastEndpoint.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Required));
        }

        [Test]
        public async Task ModelVersionLookupUsesDocumentedRoute()
        {
            var service = new SimultriaModelLookupService(
                apiClient,
                fixture.Composition,
                SimultriaEnvironmentIds.Development);

            await service.GetModelVersionAsync(34);

            Assert.That(apiClient.LastEndpoint.Path,
                Does.EndWith("/api/v2/projects/models/versions/34"));
        }

        [Test]
        public async Task ActivityLookupUsesVersionScopedRoute()
        {
            var service = new SimultriaActivityLookupService(
                apiClient,
                fixture.Composition,
                SimultriaEnvironmentIds.Development);

            await service.GetActivitiesAsync(34);

            Assert.That(apiClient.LastEndpoint.Path,
                Does.EndWith(
                    "/api/v2/projects/models/versions/34/activities"));
        }

        [Test]
        public async Task ActivityLookupAcceptsReportOwnedDtoWithoutRouteDuplication()
        {
            var service = new SimultriaActivityLookupService(
                apiClient,
                fixture.Composition,
                SimultriaEnvironmentIds.Development);

            await service.GetActivitiesAsync<ReportOwnedActivity>(34);

            Assert.That(apiClient.LastEndpoint.Path,
                Does.EndWith(
                    "/api/v2/projects/models/versions/34/activities"));
        }

        private sealed class ReportOwnedActivity
        {
        }
    }
}
