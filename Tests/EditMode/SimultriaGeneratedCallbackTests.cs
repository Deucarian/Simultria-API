using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Deucarian.API;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Generated;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaGeneratedCallbackTests
    {
        #region Fields
        private SimultriaTestComposition _fixture;
        private ApiClientSpy _client;
        private SimultriaApi _api;
        private int _mainThreadId;
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
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();
        #endregion

        #region Tests
        [Test]
        public void EveryCatalogOperationAndAsyncOverloadHasStronglyTypedCallbacks()
        {
            HashSet<ApiEndpointId> operationIds = new HashSet<ApiEndpointId>();
            foreach (Type group in typeof(SimultriaEndpoints).GetNestedTypes(BindingFlags.Public))
            {
                Type operations = typeof(SimultriaApi).GetProperty(group.Name).PropertyType;
                foreach (PropertyInfo operation in group.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    Type[] contracts = operation.PropertyType.GetGenericArguments();
                    Assert.That(contracts.Length, Is.EqualTo(2), operation.Name);
                    AssertCallback(operations, operation.Name, contracts[1], new[] { contracts[0] });
                    object definition = operation.GetValue(null);
                    ApiEndpointId endpointId = (ApiEndpointId)operation.PropertyType
                        .GetProperty("EndpointId").GetValue(definition);
                    operationIds.Add(endpointId);
                }

                foreach (MethodInfo method in operations.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                                                    | BindingFlags.DeclaredOnly))
                {
                    if (!method.Name.EndsWith("Async", StringComparison.Ordinal))
                        continue;

                    Assert.That(method.ReturnType.GetGenericTypeDefinition(), Is.EqualTo(typeof(Task<>)));
                    Type result = method.ReturnType.GetGenericArguments()[0];
                    Assert.That(result.GetGenericTypeDefinition(), Is.EqualTo(typeof(ApiResult<>)));
                    Type response = result.GetGenericArguments()[0];
                    ParameterInfo[] parameters = method.GetParameters();
                    List<Type> inputs = new List<Type>();
                    for (int index = 0; index < parameters.Length - 1; index++)
                    {
                        inputs.Add(parameters[index].ParameterType);
                    }

                    string name = method.Name.Substring(0, method.Name.Length - "Async".Length);
                    AssertCallback(operations, name, response, inputs);
                    if (parameters[0].IsOptional)
                        AssertCallback(operations, name, response, Array.Empty<Type>());
                }
            }

            List<ApiEndpointId> catalogIds = new List<ApiEndpointId>();
            foreach (ApiEndpointCatalogEntry entry in _fixture.Catalog.Endpoints)
            {
                catalogIds.Add(new ApiEndpointId(entry.EndpointId));
            }
            Assert.That(operationIds, Is.EquivalentTo(catalogIds));
            Assert.That(operationIds.Count, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator ActivityShorthandReturnsTypedResponseOnMainThread()
        {
            ModelVersionsGetActivitiesResponse response = new ModelVersionsGetActivitiesResponse();
            _client.ResponseData = response;
            CallbackResult<ModelVersionsGetActivitiesResponse> result =
                new CallbackResult<ModelVersionsGetActivitiesResponse>();
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                _api.ModelVersions.GetActivities(34, result.OnSuccess, result.OnFailure, cancellation.Token);
                yield return result.Wait();
                Assert.That(result.Data, Is.SameAs(response));
                Assert.That(result.Error, Is.Null);
                Assert.That(result.ThreadId, Is.EqualTo(_mainThreadId));
                Assert.That(_client.LastCancellationToken, Is.EqualTo(cancellation.Token));
                Assert.That(_client.LastRequest.Endpoint,
                            Is.EqualTo("https://api.example.invalid/api/v2/projects/models/versions/34/activities"));
                Assert.That(_client.LastRequest.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));
            }
        }

        [UnityTest]
        public IEnumerator TransportFailurePreservesStructuredError()
        {
            ApiError error = new ApiError { Message = "Unavailable", HttpStatusCode = 503 };
            _client.ResponseError = error;
            CallbackResult<ModelVersionsGetActivitiesResponse> result =
                new CallbackResult<ModelVersionsGetActivitiesResponse>();
            _api.ModelVersions.GetActivities(34, result.OnSuccess, result.OnFailure);
            yield return result.Wait();
            Assert.That(result.Error, Is.SameAs(error));
            Assert.That(result.SuccessCount, Is.Zero);
            Assert.That(result.ThreadId, Is.EqualTo(_mainThreadId));
        }

        [UnityTest]
        public IEnumerator InvalidPathParametersCallFailureWithoutSending()
        {
            CallbackResult<ModelVersionsGetActivitiesResponse> result =
                new CallbackResult<ModelVersionsGetActivitiesResponse>();
            _api.ModelVersions.GetActivities(0, result.OnSuccess, result.OnFailure);
            yield return result.Wait();
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Exception, Is.InstanceOf<ArgumentException>());
            Assert.That(result.SuccessCount, Is.Zero);
            Assert.That(_client.SendCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CancellationCallsFailureWithoutSending()
        {
            CallbackResult<ModelVersionsGetActivitiesResponse> result =
                new CallbackResult<ModelVersionsGetActivitiesResponse>();
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                _api.ModelVersions.GetActivities(34, result.OnSuccess, result.OnFailure, cancellation.Token);
                yield return result.Wait();
                Assert.That(result.Error.IsCancellation, Is.True);
                Assert.That(result.SuccessCount, Is.Zero);
                Assert.That(_client.SendCount, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator OptionalRequestCanBeOmittedOrSupplyTypedFilters()
        {
            CallbackResult<ProjectsGetProjectsResponse> omitted = new CallbackResult<ProjectsGetProjectsResponse>();
            _api.Projects.GetProjects(omitted.OnSuccess, omitted.OnFailure);
            yield return omitted.Wait();
            Assert.That(omitted.SuccessCount, Is.EqualTo(1));
            Assert.That(_client.LastRequest.QueryParameters, Is.Empty);

            CallbackResult<ProjectsGetProjectsResponse> filtered = new CallbackResult<ProjectsGetProjectsResponse>();
            _api.Projects.GetProjects(new ProjectsGetProjectsRequest { FilterSearch = "Example", PerPage = 25 },
                                     filtered.OnSuccess,
                                     filtered.OnFailure
            );
            yield return filtered.Wait();
            Assert.That(filtered.SuccessCount, Is.EqualTo(1));
            Assert.That(_client.LastRequest.QueryParameters["filter[search]"], Is.EqualTo("Example"));
            Assert.That(_client.LastRequest.QueryParameters["per_page"], Is.EqualTo("25"));
        }

        [UnityTest]
        public IEnumerator CallbackLoginPreservesSensitiveRequestPolicy()
        {
            AuthenticationLoginBody body = new AuthenticationLoginBody { Password = "test-only" };
            CallbackResult<AuthenticationLoginResponse> result = new CallbackResult<AuthenticationLoginResponse>();
            _api.Authentication.Login(new AuthenticationLoginRequest { Body = body },
                                      result.OnSuccess,
                                      result.OnFailure
            );
            yield return result.Wait();
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(_client.LastRequest.Body, Is.SameAs(body));
            Assert.That(_client.LastRequest.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(_client.LastRequest.SuppressLogging, Is.True);
        }

        [UnityTest]
        public IEnumerator CallbackMultipartPreservesUploadMapping()
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
            CallbackResult<ChangelogStoreChangelogItemResponse> result =
                new CallbackResult<ChangelogStoreChangelogItemResponse>();
            _api.Changelog.StoreChangelogItem(parameters, result.OnSuccess, result.OnFailure);
            yield return result.Wait();
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(_client.LastRequest.BodyFormat, Is.EqualTo(ApiRequestBodyFormat.MultipartForm));
            List<IMultipartFormSection> form = (List<IMultipartFormSection>)_client.LastRequest.Body;
            Assert.That(form.Find(section => section.sectionName == "screenshot").sectionData, Is.EqualTo(bytes));
            Assert.That(form.Exists(section => section.sectionName == "environments[development]"), Is.True);
        }

        [UnityTest]
        public IEnumerator CallbackDirectoryPreservesExplicitDirectoryAndEscapesSegments()
        {
            CallbackResult<UnityBuildVersionsGetVersionResponse> result =
                new CallbackResult<UnityBuildVersionsGetVersionResponse>();
            _api.UnityBuildVersions.GetVersion(new UnityBuildVersionsGetVersionRequest
                                              {
                                                  Id = "a/b",
                                                  Product = "report viewer",
                                                  LookupEnvironment = SimultriaUnityBuildLookupEnvironment.Development
                                              },
                                              result.OnSuccess,
                                              result.OnFailure
            );
            yield return result.Wait();
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(_client.LastRequest.Endpoint,
                        Does.StartWith(SimultriaUnityBuildDirectory.DevelopmentBaseUrl));
            Assert.That(_client.LastRequest.Endpoint, Does.EndWith("/a%2Fb/report%20viewer").IgnoreCase);
            Assert.That(_client.LastRequest.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Disabled));
            Assert.That(_client.LastRequest.SuppressLogging, Is.True);
        }
        #endregion

        #region Methods
        private static void AssertCallback(Type operations, string name, Type response, IEnumerable<Type> inputs)
        {
            List<Type> parameters = new List<Type>(inputs)
            {
                typeof(Action<>).MakeGenericType(response),
                typeof(Action<ApiError>),
                typeof(CancellationToken)
            };
            MethodInfo callback = operations.GetMethod(name, parameters.ToArray());
            Assert.That(callback, Is.Not.Null, operations.Name + "." + name);
            Assert.That(callback.ReturnType, Is.EqualTo(typeof(void)));
            ParameterInfo[] callbackParameters = callback.GetParameters();
            Assert.That(callbackParameters[callbackParameters.Length - 1].IsOptional, Is.True);
        }
        #endregion

        #region Nested Types
        private sealed class CallbackResult<T>
        {
            #region Properties
            internal T Data { get; private set; }
            internal ApiError Error { get; private set; }
            internal int SuccessCount { get; private set; }
            internal int FailureCount { get; private set; }
            internal int ThreadId { get; private set; }
            #endregion

            #region Methods
            internal void OnSuccess(T data)
            {
                Data = data;
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                SuccessCount++;
            }

            internal void OnFailure(ApiError error)
            {
                Error = error;
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                FailureCount++;
            }

            internal IEnumerator Wait()
            {
                float deadline = Time.realtimeSinceStartup + 5f;
                while (SuccessCount + FailureCount == 0 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(SuccessCount + FailureCount, Is.EqualTo(1), "Expected exactly one completion callback.");
            }
            #endregion
        }
        #endregion
    }
}
