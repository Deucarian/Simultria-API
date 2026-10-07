using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;

namespace Deucarian.Simultria.API
{
    /// <summary>
    /// Generated backend operations over a borrowed, authenticated API client.
    /// Construct once per connection and inject into application services.
    /// This facade does not own or dispose the client or authentication session.
    /// </summary>
    public sealed partial class SimultriaApi
    {
        #region Fields
        private readonly IApiClient _client;
        private readonly ApiComposition _composition;
        private readonly ApiEnvironmentId _environmentId;
        private readonly CancellationToken[] _lifetimes;
        #endregion

        #region Constructors
        public SimultriaApi(IApiClient client, ApiConnectionSettings settings, ApiEnvironmentId environmentId)
            : this(client, SimultriaApiConnectionSettingsAdapter.CreateComposition(settings), environmentId) { }

        public SimultriaApi(IApiClient client, ApiComposition composition, ApiEnvironmentId environmentId)
            : this(client, composition, environmentId, Array.Empty<CancellationToken>()) { }

        public SimultriaApi(IApiClient client,
                           ApiComposition composition,
                           ApiEnvironmentId environmentId,
                           CancellationToken lifetime)
            : this(client, composition, environmentId, new[] { lifetime }) { }

        public SimultriaApi(IApiClient client,
                           ApiConnectionSettings settings,
                           ApiEnvironmentId environmentId,
                           CancellationToken lifetime)
            : this(client, SimultriaApiConnectionSettingsAdapter.CreateComposition(settings), environmentId, lifetime) { }

        private SimultriaApi(IApiClient client,
                            ApiComposition composition,
                            ApiEnvironmentId environmentId,
                            CancellationToken[] lifetimes)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            if (composition.CatalogId != SimultriaCatalogIds.ApiV2)
            {
                throw new ArgumentException("The Simultria API v2 catalog is required.", nameof(composition));
            }

            ApiEnvironmentStatus status = composition.GetEnvironmentStatus(environmentId);
            if (!status.IsResolved)
            {
                throw new ArgumentException(status.Message, nameof(environmentId));
            }

            _environmentId = environmentId;
            _lifetimes = lifetimes;
            InitializeOperations();
        }
        #endregion

        #region Properties
        public ApiEnvironmentId EnvironmentId => _environmentId;

        internal IApiClient Client => _client;
        internal ApiComposition Composition => _composition;
        #endregion

        #region Methods
        partial void InitializeOperations();

        /// <summary>Share this connection while also canceling requests when the supplied owner lifetime ends.</summary>
        public SimultriaApi WithLifetime(CancellationToken lifetime)
        {
            CancellationToken[] lifetimes = new CancellationToken[_lifetimes.Length + 1];
            Array.Copy(_lifetimes, lifetimes, _lifetimes.Length);
            lifetimes[lifetimes.Length - 1] = lifetime;
            return new SimultriaApi(_client, _composition, _environmentId, lifetimes);
        }

        /// <summary>Resolve a typed operation for inspection without sending a network request.</summary>
        public ApiRequest CreateRequest<TRequest, TResponse>(SimultriaOperation<TRequest, TResponse> operation,
                                                            TRequest parameters)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            ApiEndpoint endpoint = operation.ResolveEndpoint(_composition, _environmentId, parameters);
            return operation.CreateRequest(endpoint, parameters);
        }

        /// <summary>The operation fixes both the request and response types at the call site.</summary>
        public Task<ApiResult<TResponse>> SendAsync<TRequest, TResponse>(
            SimultriaOperation<TRequest, TResponse> operation,
            TRequest parameters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfLifetimeEnded();
            return ExecuteAsync<TResponse>(CreateRequest(operation, parameters), cancellationToken);
        }

        /// <summary>Send a typed operation with exactly one success or failure callback on the Unity main thread.</summary>
        public void Send<TRequest, TResponse>(SimultriaOperation<TRequest, TResponse> operation,
                                             TRequest parameters,
                                             Action<TResponse> onSuccess,
                                             Action<ApiError> onFailure,
                                             CancellationToken cancellationToken = default)
        {
            Run(token => SendAsync(operation, parameters, token), onSuccess, onFailure, cancellationToken);
        }

        internal void Run<TResponse>(Func<CancellationToken, Task<ApiResult<TResponse>>> operation,
                                     Action<TResponse> onSuccess,
                                     Action<ApiError> onFailure,
                                     CancellationToken cancellationToken)
        {
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
            CancellationTokenSource lifetime = LinkLifetimes(cancellationToken, out CancellationToken token);
            try
            {
                ApiCallbacks.Run(() => operation(token),
                                 response =>
                                 {
                                     try
                                     {
                                         if (token.IsCancellationRequested)
                                         {
                                             onFailure(CancellationError());
                                         }
                                         else
                                         {
                                             onSuccess(response);
                                         }
                                     }
                                     finally { lifetime?.Dispose(); }
                                 },
                                 failure =>
                                 {
                                     try { onFailure(token.IsCancellationRequested ? CancellationError() : failure); }
                                     finally { lifetime?.Dispose(); }
                                 },
                                 token
                );
            }
            catch
            {
                lifetime?.Dispose();
                throw;
            }
        }
        #endregion
    }
}
