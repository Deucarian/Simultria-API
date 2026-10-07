using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Deucarian.API;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaModelTreeApiTests
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
            _client = new ApiClientSpy { ResponseData = Tree() };
            _api = new SimultriaApi(_client, _fixture.Composition, SimultriaEnvironmentIds.Development);
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();
        #endregion

        #region Tests
        [Test]
        public async Task InjectedResolverPreservesModelSelectionAndFieldProjection()
        {
            SimultriaViewerModelResolveResult result = await new SimultriaViewerModelResolver(_api).ResolveAsync(1, 2);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ModelVersionId, Is.EqualTo(17));
            Assert.That(result.UsedActiveVersion, Is.True);
            Assert.That(_client.LastEndpoint, Is.Null, "Use the typed request pipeline.");
            Assert.That(_client.LastRequest.Endpoint, Does.EndWith("/api/v2/projects/1"));
            Assert.That(_client.LastRequest.QueryParameters["included_fields"], Does.Contain("models.active_version"));
            Assert.That(_client.LastRequest.QueryParameters["included_fields"], Does.Contain("projects.sub_projects"));
            Assert.That(_client.LastRequest.Authentication, Is.EqualTo(ApiAuthenticationRequirement.Required));
        }

        [UnityTest]
        public IEnumerator CallbackAndAsyncModelTreeReturnTheSameContract()
        {
            Completion completion = new Completion();
            int mainThread = Thread.CurrentThread.ManagedThreadId;
            _api.Projects.GetModelTree(1, tree => completion.Success(tree.Data.Id), completion.Failure);
            yield return completion.Wait();
            Task<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>> task = _api.Projects.GetModelTreeAsync(1);
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(completion.Value, Is.EqualTo(task.Result.Data.Data.Id));
            Assert.That(completion.Thread, Is.EqualTo(mainThread));
            Assert.That(completion.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ModelResolverPreservesDomainFailureCodeThroughCallbacks()
        {
            Completion completion = new Completion();
            new SimultriaViewerModelResolver(_api).Resolve(1, 999,
                                                         result => completion.Success(result.ModelVersionId),
                                                         completion.Failure
            );
            yield return completion.Wait();
            Assert.That(completion.Error.BackendCode, Is.EqualTo(SimultriaViewerModelErrorCodes.ModelNotFound));
            Assert.That(completion.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExpiredOwnerDoesNotSendModelRequests()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            {
                SimultriaViewerModelResolver resolver = new SimultriaViewerModelResolver(_api.WithLifetime(owner.Token));
                owner.Cancel();
                Completion completion = new Completion();
                resolver.Resolve(1, 2, result => completion.Success(result.ModelVersionId), completion.Failure);
                yield return completion.Wait();
                Assert.That(completion.Error.IsCancellation, Is.True);
                Assert.That(_client.SendCount, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator ConnectionEndingRejectsLateModelResolutionExactlyOnce()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            {
                TaskCompletionSource<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>> pending =
                    new TaskCompletionSource<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>>();
                _client.PendingTask = pending.Task;
                Completion completion = new Completion();
                new SimultriaViewerModelResolver(_api.WithLifetime(owner.Token)).Resolve(
                    1, 2, result => completion.Success(result.ModelVersionId), completion.Failure
                );
                owner.Cancel();
                yield return completion.Wait();
                Assert.That(completion.Error.IsCancellation, Is.True);
                Assert.That(_client.LastCancellationToken.IsCancellationRequested, Is.True);
                pending.SetResult(ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>.Success(
                    Tree(), HttpMethod.GET, 200, null, null
                ));
                yield return null;
                yield return null;
                Assert.That(completion.Count, Is.EqualTo(1));
            }
        }
        #endregion

        #region Methods
        private static SimultriaResourceResponse<SimultriaProjectDto> Tree() =>
            new SimultriaResourceResponse<SimultriaProjectDto>
            {
                Data = new SimultriaProjectDto
                {
                    Id = 1,
                    Models = new List<SimultriaModelDto>
                    {
                        new SimultriaModelDto
                        {
                            Id = 2,
                            ActiveVersion = new SimultriaModelVersionDto
                            {
                                Id = 17, DownloadUrl = "https://model.example.invalid/model"
                            }
                        }
                    }
                }
            };
        #endregion

        #region Nested Types
        private sealed class Completion
        {
            internal int Count { get; private set; }
            internal int Thread { get; private set; }
            internal int Value { get; private set; }
            internal ApiError Error { get; private set; }
            internal void Success(int value) { Count++; Thread = System.Threading.Thread.CurrentThread.ManagedThreadId; Value = value; }
            internal void Failure(ApiError error) { Count++; Thread = System.Threading.Thread.CurrentThread.ManagedThreadId; Error = error; }
            internal IEnumerator Wait()
            {
                float deadline = Time.realtimeSinceStartup + 5;
                while (Count == 0 && Time.realtimeSinceStartup < deadline) { yield return null; }
                Assert.That(Count, Is.EqualTo(1), "A callback must complete promptly.");
            }
        }
        #endregion
    }
}
