using System;
using NUnit.Framework;
using Deucarian.API;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Generated;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaEndpointResolutionTests
    {
        #region Fields
        private SimultriaTestComposition _fixture;
        private ApiClientSpy _client;
        private SimultriaApi _api;
        #endregion

        #region Setup and Teardown
        [SetUp]
        public void SetUp()
        {
            _fixture = new SimultriaTestComposition();
            _client = new ApiClientSpy();
            _api = new SimultriaApi(_client, _fixture.Composition, SimultriaEnvironmentIds.Development);
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();
        #endregion

        #region Tests
        [Test]
        public void AuthenticationEndpointsCarryDocumentedMethodAndAuthRules()
        {
            ApiRequest login = _api.CreateRequest(SimultriaEndpoints.Authentication.Login,
                                                  new AuthenticationLoginRequest()
            );
            ApiRequest validation = _api.CreateRequest(SimultriaEndpoints.Authentication.Validate,
                                                       new AuthenticationValidateRequest()
            );

            Assert.That(login.Endpoint, Does.EndWith("/api/v2/login"));
            Assert.That(login.Method, Is.EqualTo(HttpMethod.POST));
            Assert.That(login.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(validation.Endpoint, Does.EndWith("/api/v2/auth/validate"));
            Assert.That(validation.Method, Is.EqualTo(HttpMethod.GET));
            Assert.That(validation.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));
            Assert.That(login.SuppressLogging, Is.True);
            Assert.That(validation.SuppressLogging, Is.True);
        }

        [Test]
        public void ViewerLookupRoutesResolvePositiveIds()
        {
            ApiRequest project = _api.CreateRequest(SimultriaEndpoints.Projects.GetProject,
                                                    new ProjectsGetProjectRequest { Id = 12 }
            );
            ApiRequest models = _api.CreateRequest(SimultriaEndpoints.Projects.GetModels,
                                                   new ProjectsGetModelsRequest { ProjectId = 12 }
            );
            ApiRequest version = _api.CreateRequest(SimultriaEndpoints.ModelVersions.GetModelVersion,
                                                    new ModelVersionsGetModelVersionRequest { Id = 34 }
            );
            ApiRequest activities = _api.CreateRequest(SimultriaEndpoints.ModelVersions.GetActivities,
                                                       new ModelVersionsGetActivitiesRequest { VersionId = 34 }
            );
            ApiRequest activity = _api.CreateRequest(SimultriaEndpoints.ModelVersions.GetActivity,
                                                     new ModelVersionsGetActivityRequest { VersionId = 34, Id = 56 }
            );

            Assert.That(project.Endpoint, Does.EndWith("/api/v2/projects/12"));
            Assert.That(models.Endpoint, Does.EndWith("/api/v2/projects/12/models"));
            Assert.That(version.Endpoint, Does.EndWith("/api/v2/projects/models/versions/34"));
            Assert.That(activities.Endpoint, Does.EndWith("/api/v2/projects/models/versions/34/activities"));
            Assert.That(activity.Endpoint, Does.EndWith("/api/v2/projects/models/versions/34/activities/56"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveResourceIdIsRejectedBeforeTransport(int projectId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _api.Projects.GetProjectAsync(projectId));
            Assert.That(_client.SendCount, Is.Zero);
        }

        [Test]
        public void HandwrittenEndpointCatalogHasBeenRemoved()
        {
            Assert.That(typeof(SimultriaApi).Assembly.GetType(
                            "Deucarian.Simultria.API.Endpoints.SimultriaEndpointCatalog"
                        ),
                        Is.Null
            );
        }
        #endregion
    }
}
