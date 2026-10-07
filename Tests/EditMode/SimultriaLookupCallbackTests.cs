using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaLookupCallbackTests
    {
        #region Fields
        private ApiClientSpy _client;
        private SimultriaTestComposition _fixture;
        private SimultriaLookupContext _context;
        #endregion

        #region Setup and Teardown
        [SetUp]
        public void SetUp()
        {
            _client = new ApiClientSpy();
            _fixture = new SimultriaTestComposition();
            _context = new SimultriaLookupContext(_client,
                                                _fixture.Composition,
                                                SimultriaEnvironmentIds.Development
            );
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator ProjectCallbackPreservesResponseAndRoute()
        {
            SimultriaProjectLookupService service = new SimultriaProjectLookupService(_context);
            SimultriaResourceResponse<SimultriaProjectDto> expected =
                new SimultriaResourceResponse<SimultriaProjectDto>();
            _client.ResponseData = expected;
            SimultriaResourceResponse<SimultriaProjectDto> actual = null;
            int completions = 0;
            ApiError failure = null;
            service.GetProject(12,
                               value => { actual = value; completions++; },
                               error => { failure = error; completions++; }
            );
            yield return WaitFor(() => completions != 0);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(failure, Is.Null);
            Assert.That(_client.LastEndpoint.Path, Does.EndWith("/api/v2/projects/12"));
            Assert.That(_client.SendCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ModelCallbackPreservesStructuredFailure()
        {
            SimultriaModelLookupService service = new SimultriaModelLookupService(_context);
            ApiError expected = new ApiError { Message = "Unavailable", HttpStatusCode = 503, IsTimeout = true };
            _client.ResponseError = expected;
            int successes = 0;
            int failures = 0;
            ApiError actual = null;
            service.GetModelVersion(34, _ => successes++, error => { actual = error; failures++; });
            yield return WaitFor(() => failures + successes != 0);
            Assert.That(successes, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(actual, Is.SameAs(expected));
        }

        [UnityTest]
        public IEnumerator CustomActivityResponseSupportsCancellationWithoutSending()
        {
            SimultriaActivityLookupService service = new SimultriaActivityLookupService(_context);
            int successes = 0;
            int failures = 0;
            ApiError actual = null;
            service.GetActivities<CustomActivity>(34,
                                                  _ => successes++,
                                                  error => { actual = error; failures++; },
                                                  new CancellationToken(true)
            );
            yield return WaitFor(() => failures + successes != 0);
            Assert.That(successes, Is.Zero);
            Assert.That(failures, Is.EqualTo(1));
            Assert.That(actual.IsCancellation, Is.True);
            Assert.That(_client.SendCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InvalidBuildLookupRoutesValidationToFailure()
        {
            SimultriaUnityBuildVersionLookupService service = new SimultriaUnityBuildVersionLookupService(_client);
            int completions = 0;
            ApiError actual = null;
            service.GetBuildVersion(string.Empty,
                                    "test-product",
                                    _ => completions++,
                                    error => { actual = error; completions++; }
            );
            yield return WaitFor(() => completions != 0);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(actual, Is.Not.Null);
            Assert.That(_client.SendCount, Is.Zero);
        }

        [Test]
        public void EveryLegacyEndpointTaskHasTypedCallbacks()
        {
            Type[] services =
            {
                typeof(SimultriaProjectLookupService),
                typeof(SimultriaModelLookupService),
                typeof(SimultriaActivityLookupService),
                typeof(SimultriaUnityBuildVersionLookupService),
                typeof(SimultriaLookupContext)
            };
            int checkedMethods = 0;
            foreach (Type service in services)
            {
                MethodInfo[] methods = service.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (!method.ReturnType.IsGenericType
                        || method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                        continue;

                    Type result = method.ReturnType.GetGenericArguments()[0];
                    if (!result.IsGenericType || result.GetGenericTypeDefinition() != typeof(ApiResult<>))
                        continue;

                    string callbackName = method.Name.Substring(0, method.Name.Length - "Async".Length);
                    Type response = result.GetGenericArguments()[0];
                    string[] expected = method.GetParameters()
                        .Where(parameter => parameter.ParameterType != typeof(CancellationToken))
                        .Select(parameter => parameter.ParameterType.ToString())
                        .Concat(new[]
                        {
                            typeof(Action<>).MakeGenericType(response).ToString(),
                            typeof(Action<ApiError>).ToString(),
                            typeof(CancellationToken).ToString()
                        }).ToArray();
                    bool found = methods.Any(candidate => candidate.Name == callbackName
                        && candidate.ReturnType == typeof(void)
                        && candidate.GetGenericArguments().Length == method.GetGenericArguments().Length
                        && candidate.GetParameters().Select(parameter => parameter.ParameterType.ToString())
                            .SequenceEqual(expected));
                    Assert.That(found, Is.True, service.Name + "." + method.Name + " must offer typed callbacks.");
                    checkedMethods++;
                }
            }
            Assert.That(checkedMethods, Is.GreaterThanOrEqualTo(13));
        }
        #endregion

        #region Methods
        private static IEnumerator WaitFor(Func<bool> completed)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!completed() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(completed(), Is.True, "The endpoint must invoke a completion callback.");
        }
        #endregion

        #region Nested Types
        private sealed class CustomActivity { }
        #endregion
    }
}
