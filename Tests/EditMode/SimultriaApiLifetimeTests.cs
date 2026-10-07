using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Generated;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiLifetimeTests
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
        public void TearDown() { _fixture.Dispose(); }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator ConnectionCancellationReachesTransportAndCompletesOnceBeforeLateFailure()
        {
            using (CancellationTokenSource lifetime = new CancellationTokenSource())
            {
                TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>> pending = Pending();
                SimultriaApi api = new SimultriaApi(_client, _fixture.Composition,
                                                  SimultriaEnvironmentIds.Development, lifetime.Token
                );
                Completion completion = new Completion();
                api.ModelVersions.GetActivities(34, completion.Success, completion.Failure);
                Assert.That(_client.SendCount, Is.EqualTo(1));
                lifetime.Cancel();
                yield return completion.Wait();
                Assert.That(completion.Error.IsCancellation, Is.True);
                Assert.That(_client.LastCancellationToken.IsCancellationRequested, Is.True);
                pending.SetException(new InvalidOperationException("Late failure."));
                yield return null;
                yield return null;
                Assert.That(completion.Count, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator NestedOwnerCancellationDoesNotCancelTheConnectionOrAnIndependentConsumer()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            using (CancellationTokenSource shorter = new CancellationTokenSource())
            {
                SimultriaApi owned = _api.WithLifetime(owner.Token).WithLifetime(shorter.Token);
                owner.Cancel();
                Completion canceled = new Completion();
                Completion independent = new Completion();
                owned.ModelVersions.GetActivities(34, canceled.Success, canceled.Failure);
                _api.ModelVersions.GetActivities(34, independent.Success, independent.Failure);
                yield return canceled.Wait();
                yield return independent.Wait();
                Assert.That(canceled.Error.IsCancellation, Is.True);
                Assert.That(independent.Error, Is.Null);
                Assert.That(_client.SendCount, Is.EqualTo(1));
                Assert.That(owned.EnvironmentId, Is.EqualTo(_api.EnvironmentId));
            }
        }

        [UnityTest]
        public IEnumerator OwnerCancellationBetweenTransportAndCallbackSuppressesStaleSuccess()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            {
                Completion completion = new Completion();
                _api.WithLifetime(owner.Token).ModelVersions.GetActivities(34, completion.Success, completion.Failure);
                Assert.That(completion.Count, Is.Zero, "Completion must be posted.");
                owner.Cancel();
                yield return completion.Wait();
                Assert.That(completion.Error.IsCancellation, Is.True);
                Assert.That(completion.Successes, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator ExplicitRequestCancellationRetainsOwnerLifetimeAndCancelsExactlyOnce()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            using (CancellationTokenSource request = new CancellationTokenSource())
            {
                TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>> pending = Pending();
                Completion completion = new Completion();
                _api.WithLifetime(owner.Token).ModelVersions.GetActivities(34, completion.Success, completion.Failure,
                                                                          request.Token
                );
                request.Cancel();
                yield return completion.Wait();
                Assert.That(completion.Error.IsCancellation, Is.True);
                Assert.That(owner.IsCancellationRequested, Is.False);
                pending.SetResult(new ApiResult<ModelVersionsGetActivitiesResponse> { IsSuccess = true });
                yield return null;
                yield return null;
                Assert.That(completion.Count, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator AsyncCallAlsoFinishesWhenAnOwnerEndsWithoutWaitingForTransport()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            {
                TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>> pending = Pending();
                Task<ApiResult<ModelVersionsGetActivitiesResponse>> task =
                    _api.WithLifetime(owner.Token).ModelVersions.GetActivitiesAsync(34);
                owner.Cancel();
                float deadline = Time.realtimeSinceStartup + 5f;
                while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(task.IsCompleted, Is.True);
                Assert.That(task.Result.Error.IsCancellation, Is.True);
                pending.SetResult(new ApiResult<ModelVersionsGetActivitiesResponse> { IsSuccess = true });
                yield return null;
            }
        }

        [Test]
        public void ExpiredConnectionCannotSendOrSilentlyChangeEnvironment()
        {
            using (CancellationTokenSource owner = new CancellationTokenSource())
            {
                SimultriaApi api = _api.WithLifetime(owner.Token);
                owner.Cancel();
                Assert.Throws<OperationCanceledException>(() => api.ModelVersions.GetActivitiesAsync(34));
                Assert.That(_client.SendCount, Is.Zero);
                Assert.That(api.EnvironmentId, Is.EqualTo(SimultriaEnvironmentIds.Development));
            }
        }
        #endregion

        #region Methods
        private TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>> Pending()
        {
            TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>> pending =
                new TaskCompletionSource<ApiResult<ModelVersionsGetActivitiesResponse>>();
            _client.PendingTask = pending.Task;
            return pending;
        }
        #endregion

        #region Nested Types
        private sealed class Completion
        {
            #region Fields
            private readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            private int _completionThreadId;
            #endregion

            #region Properties
            internal int Count { get; private set; }
            internal int Successes { get; private set; }
            internal ApiError Error { get; private set; }
            #endregion

            #region Methods
            internal void Success(ModelVersionsGetActivitiesResponse response)
            {
                _completionThreadId = Thread.CurrentThread.ManagedThreadId;
                Count++;
                Successes++;
            }
            internal void Failure(ApiError error)
            {
                _completionThreadId = Thread.CurrentThread.ManagedThreadId;
                Count++;
                Error = error;
            }
            internal IEnumerator Wait()
            {
                float deadline = Time.realtimeSinceStartup + 5f;
                while (Count == 0 && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(Count, Is.EqualTo(1));
                Assert.That(_completionThreadId, Is.EqualTo(_mainThreadId));
            }
            #endregion
        }
        #endregion
    }
}
