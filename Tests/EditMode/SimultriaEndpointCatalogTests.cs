using Deucarian.API;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Endpoints;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaEndpointCatalogTests
    {
        private SimultriaTestComposition fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new SimultriaTestComposition();
        }

        [TearDown]
        public void TearDown()
        {
            fixture.Dispose();
        }

        [Test]
        public void AuthenticationEndpointsCarryDocumentedMethodAndAuthRules()
        {
            ApiEndpoint login = SimultriaEndpointCatalog.Login(
                fixture.Composition,
                SimultriaEnvironmentIds.Development);
            ApiEndpoint validation =
                SimultriaEndpointCatalog.ValidateAuthentication(
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

            Assert.That(login.Path, Does.EndWith("/api/v2/login"));
            Assert.That(login.Method, Is.EqualTo(HttpMethod.POST));
            Assert.That(login.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(validation.Path,
                Does.EndWith("/api/v2/auth/validate"));
            Assert.That(validation.Method, Is.EqualTo(HttpMethod.GET));
            Assert.That(validation.Authentication,
                Is.EqualTo(ApiAuthenticationRequirement.Required));
            Assert.That(login.SuppressLogging, Is.True);
            Assert.That(validation.SuppressLogging, Is.True);
        }

        [Test]
        public void ViewerLookupRoutesResolvePositiveIds()
        {
            Assert.That(
                SimultriaEndpointCatalog.Project(
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development,
                    12).Path,
                Does.EndWith("/api/v2/projects/12"));
            Assert.That(
                SimultriaEndpointCatalog.ProjectModels(
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development,
                    12).Path,
                Does.EndWith("/api/v2/projects/12/models"));
            Assert.That(
                SimultriaEndpointCatalog.ModelVersion(
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development,
                    34).Path,
                Does.EndWith("/api/v2/projects/models/versions/34"));
            Assert.That(
                SimultriaEndpointCatalog
                    .ModelVersionActivities(
                        fixture.Composition,
                        SimultriaEnvironmentIds.Development,
                        34)
                    .Path,
                Does.EndWith(
                    "/api/v2/projects/models/versions/34/activities"));
            Assert.That(
                SimultriaEndpointCatalog
                    .ModelVersionActivity(
                        fixture.Composition,
                        SimultriaEnvironmentIds.Development,
                        34,
                        56)
                    .Path,
                Does.EndWith(
                    "/api/v2/projects/models/versions/34/activities/56"));
        }

        [Test]
        public void NonPositiveResourceIdIsRejectedBeforeTransport()
        {
            Assert.That(
                () => SimultriaEndpointCatalog.Project(
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development,
                    0),
                Throws.ArgumentException);
        }
    }
}
