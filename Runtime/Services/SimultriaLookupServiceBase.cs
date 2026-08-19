using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>Shared injected transport/environment base for lookup services.</summary>
    public abstract class SimultriaLookupServiceBase
    {
        private readonly IApiClient apiClient;

        protected SimultriaLookupServiceBase(
            IApiClient client,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            apiClient = client ?? throw new ArgumentNullException(nameof(client));
            Composition = composition ??
                throw new ArgumentNullException(nameof(composition));
            EnvironmentStatus = composition.GetEnvironmentStatus(environmentId);
            if (!EnvironmentStatus.IsResolved)
            {
                throw new InvalidOperationException(EnvironmentStatus.Message);
            }

            EnvironmentId = environmentId;
        }

        public ApiComposition Composition { get; }

        public ApiEnvironmentId EnvironmentId { get; }

        public ApiEnvironmentStatus EnvironmentStatus { get; }

        protected Task<ApiResult<T>> SendAsync<T>(
            ApiEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            return apiClient.SendAsync<T>(endpoint, cancellationToken);
        }
    }
}
