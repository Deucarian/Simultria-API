using System;
using Deucarian.API;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API
{
    /// <summary>A generated operation identity with a fixed parameter and response contract.</summary>
    public sealed class SimultriaOperation<TRequest, TResponse> : EndpointKey<TRequest, TResponse>
    {
        #region Fields
        private readonly Func<ApiEndpoint, TRequest, ApiRequest> _createRequest;
        private readonly Func<TRequest, ApiEndpoint> _resolveEndpoint;
        #endregion

        #region Constructors
        internal SimultriaOperation(string id,
                                   string routeTemplate,
                                   Func<ApiEndpoint, TRequest, ApiRequest> createRequest,
                                   Func<TRequest, ApiEndpoint> resolveEndpoint = null) : base(id)
        {
            EndpointId = new ApiEndpointId(id);
            RouteTemplate = routeTemplate;
            _createRequest = createRequest ?? throw new ArgumentNullException(nameof(createRequest));
            _resolveEndpoint = resolveEndpoint;
        }
        #endregion

        #region Properties
        public ApiEndpointId EndpointId { get; }
        public string RouteTemplate { get; }
        #endregion

        #region Methods
        // Existing lookup services use the endpoint transport overload and their own response DTOs.
        // Keep that contract while letting the generated operation own every route and parameter mapping.
        internal ApiEndpoint CreateEndpoint(Deucarian.API.Core.ApiComposition composition,
                                            ApiEnvironmentId environmentId,
                                            TRequest parameters)
        {
            ApiRequest request = CreateRequest(ResolveEndpoint(composition, environmentId, parameters), parameters);
            if (request.Body != null)
            {
                throw new InvalidOperationException("Requests with a body must use SimultriaApi.SendAsync.");
            }

            return new ApiEndpoint(request.Endpoint,
                                   request.Method,
                                   request.Authentication,
                                   request.TimeoutSeconds,
                                   request.Headers,
                                   request.QueryParameters,
                                   request.ResponseFormat,
                                   request.RequestPolicy,
                                   request.SuppressLogging,
                                   request.JsonPropertyNamingOverride
            );
        }

        internal ApiEndpoint ResolveEndpoint(Deucarian.API.Core.ApiComposition composition,
                                             ApiEnvironmentId environmentId,
                                             TRequest parameters)
        {
            if (ReferenceEquals(parameters, null)) throw new ArgumentNullException(nameof(parameters));
            return _resolveEndpoint != null
                ? _resolveEndpoint(parameters)
                : composition.ResolveEndpoint(environmentId, EndpointId).Endpoint;
        }

        internal ApiRequest CreateRequest(ApiEndpoint endpoint, TRequest parameters)
        {
            if (ReferenceEquals(parameters, null)) throw new ArgumentNullException(nameof(parameters));
            return _createRequest(endpoint, parameters);
        }
        #endregion
    }
}
