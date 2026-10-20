using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Deucarian.API;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Generated;
using UnityEngine;
using UnityEngine.Networking;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaGeneratedClientTests
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
            ApiEndpointCatalog catalog = Resources.Load<ApiEndpointCatalog>(
                "Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog"
            );
            Assert.That(catalog, Is.Not.Null);
            _fixture.Catalog.Endpoints.Clear();
            _fixture.Catalog.Endpoints.AddRange(catalog.Endpoints);
            _client = new ApiClientSpy();
            _api = new SimultriaApi(_client, _fixture.Settings, SimultriaEnvironmentIds.Development);
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();
        #endregion

        #region Tests
        [Test]
        public async Task ActivityConvenienceCallFixesResponseTypeAndUsesConfiguredHost()
        {
            ModelVersionsGetActivitiesResponse response = new ModelVersionsGetActivitiesResponse();
            _client.ResponseData = response;
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                ApiResult<ModelVersionsGetActivitiesResponse> result =
                    await _api.ModelVersions.GetActivitiesAsync(34, cancellation.Token);

                Assert.That(result.Data, Is.SameAs(response));
                Assert.That(_client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(_client.LastRequest.Endpoint,
                            Is.EqualTo("https://api.example.invalid/api/v2/projects/models/versions/34/activities"));
                Assert.That(_client.LastRequest.Method, Is.EqualTo(HttpMethod.GET));
                Assert.That(_client.LastRequest.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));
            }
        }

        [Test]
        public void TypedEndpointCanBeInspectedWithoutDispatchAndRejectsMissingParameters()
        {
            ApiRequest request = _api.CreateRequest(SimultriaEndpoints.ModelVersions.GetActivities,
                                                    new ModelVersionsGetActivitiesRequest { VersionId = 34 }
            );
            Assert.That(request.Endpoint, Does.EndWith("/34/activities"));
            Assert.That(_client.SendCount, Is.Zero);
            Assert.Throws<ArgumentException>(() => _api.CreateRequest(
                SimultriaEndpoints.ModelVersions.GetActivities, new ModelVersionsGetActivitiesRequest()
            ));
        }

        [Test]
        public async Task CancellationBeforeDispatchDoesNotCallTransport()
        {
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try
                {
                    await _api.ModelVersions.GetActivitiesAsync(34, cancellation.Token);
                    Assert.Fail("Expected cancellation.");
                }
                catch (OperationCanceledException)
                {
                    Assert.That(_client.SendCount, Is.Zero);
                }
            }
        }

        [Test]
        public async Task TransportErrorsArePreserved()
        {
            ApiError error = new ApiError { Message = "Unavailable", HttpStatusCode = 503 };
            _client.ResponseError = error;
            ApiResult<ModelVersionsGetActivitiesResponse> result = await _api.ModelVersions.GetActivitiesAsync(34);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.SameAs(error));
        }

        [Test]
        public void QueryParametersPreserveWireNamesAndOmitUnsetValues()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
                ApiRequest request = _api.CreateRequest(SimultriaEndpoints.Projects.GetProjects,
                                                        new ProjectsGetProjectsRequest
                                                        {
                                                            FilterSearch = "a & b",
                                                            FilterHasModels = false,
                                                            PerPage = 25
                                                        }
                );
                Assert.That(request.QueryParameters["filter[search]"], Is.EqualTo("a & b"));
                Assert.That(request.QueryParameters["filter[has_models]"], Is.EqualTo("false"));
                Assert.That(request.QueryParameters["per_page"], Is.EqualTo("25"));
                Assert.That(request.QueryParameters.Count, Is.EqualTo(3));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ExplicitNullAndFalseSurviveSerializerDefaultsWhileUnsetFieldsAreOmitted()
        {
            AuthenticationLoginBody body = new AuthenticationLoginBody { Password = null };
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            JObject json = JObject.Parse(JsonConvert.SerializeObject(body, settings));
            Assert.That(json.ContainsKey("password"), Is.True);
            Assert.That(json["password"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(json.ContainsKey("pin"), Is.False);
            ChangelogStoreChangelogItemBodyEnvironments environments =
                new ChangelogStoreChangelogItemBodyEnvironments { Development = false };
            JObject flags = JObject.Parse(JsonConvert.SerializeObject(environments, settings));
            Assert.That(flags.ContainsKey("development"), Is.True);
            Assert.That(flags.ContainsKey("production"), Is.False);
        }

        [Test]
        public void JsonBodyAndSensitiveEndpointPolicyArePreserved()
        {
            AuthenticationLoginBody body = new AuthenticationLoginBody { Password = "test-only" };
            ApiRequest request = _api.CreateRequest(SimultriaEndpoints.Authentication.Login,
                                                    new AuthenticationLoginRequest { Body = body }
            );
            Assert.That(request.Body, Is.SameAs(body));
            Assert.That(request.BodyFormat, Is.EqualTo(ApiRequestBodyFormat.Json));
            Assert.That(request.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(request.SuppressLogging, Is.True);
        }

        [Test]
        public void MultipartMapsFilesAndNestedFieldsWithTheirContractNames()
        {
            byte[] bytes = { 1, 2, 3 };
            ChangelogStoreChangelogItemRequest parameters = new ChangelogStoreChangelogItemRequest
            {
                Body = new ChangelogStoreChangelogItemBody
                {
                    Description = "Example",
                    Type = "feature",
                    Environments = new ChangelogStoreChangelogItemBodyEnvironments { Development = true },
                    Screenshot = new SimultriaUpload(bytes, "example.png", "image/png")
                }
            };
            ApiRequest request = _api.CreateRequest(SimultriaEndpoints.Changelog.StoreChangelogItem, parameters);
            Assert.That(request.BodyFormat, Is.EqualTo(ApiRequestBodyFormat.MultipartForm));
            List<IMultipartFormSection> form = (List<IMultipartFormSection>)request.Body;
            IMultipartFormSection file = form.Find(section => section.sectionName == "screenshot");
            Assert.That(file.sectionData, Is.EqualTo(bytes));
            Assert.That(file.fileName, Is.EqualTo("example.png"));
            Assert.That(file.contentType, Is.EqualTo("image/png"));
            Assert.That(form.Exists(section => section.sectionName == "environments[development]"), Is.True);
        }

        [Test]
        public void ResponsesPreserveKnownFieldsAndUnspecifiedNestedPayloads()
        {
            ModelVersionsGetActivitiesResponse response = JsonConvert.DeserializeObject<ModelVersionsGetActivitiesResponse>(
                "{\"data\":[{\"id\":34,\"title\":\"Work\",\"issues\":[{\"custom\":1}],\"future_field\":true}]}"
            );
            Assert.That(response.Data[0].Id, Is.EqualTo(34));
            Assert.That(response.Data[0].Issues[0]["custom"].Value<int>(), Is.EqualTo(1));
            Assert.That(response.Data[0].AdditionalProperties["future_field"].Value<bool>(), Is.True);
        }

        [Test]
        public void BuildDiscoveryUsesExplicitDirectoryAndEscapesSegments()
        {
            ApiRequest request = _api.CreateRequest(SimultriaEndpoints.UnityBuildVersions.GetVersion,
                                                    new UnityBuildVersionsGetVersionRequest
                                                    {
                                                        Id = "a/b",
                                                        Product = "report viewer"
                                                    }
            );
            Assert.That(request.Endpoint, Does.Not.StartWith("https://api.example.invalid"));
            Assert.That(request.Endpoint, Does.EndWith("/a%2Fb/report%20viewer").IgnoreCase);
            Assert.That(request.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(request.SuppressLogging, Is.True);
        }

        [Test]
        public void UnconfiguredEnvironmentFailsDuringConstruction()
        {
            Assert.Throws<ArgumentException>(() =>
                new SimultriaApi(_client, _fixture.Composition, SimultriaEnvironmentIds.Production));
        }
        #endregion
    }
}
