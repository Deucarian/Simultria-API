using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>
    /// Reads the fixed central Production directory without a session or a
    /// project-owned directory profile. The returned environment is not defaulted.
    /// </summary>
    public sealed class SimultriaUnityBuildVersionLookupService
    {
        private readonly IApiClient apiClient;

        public SimultriaUnityBuildVersionLookupService(IApiClient apiClient)
        {
            this.apiClient = apiClient ??
                throw new ArgumentNullException(nameof(apiClient));
        }

        [Obsolete("The build directory is fixed. Use the IApiClient-only constructor.")]
        public SimultriaUnityBuildVersionLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId directoryEnvironmentId)
            : this(apiClient)
        {
            legacyComposition = composition;
            legacyEnvironmentId = directoryEnvironmentId;
        }

        private readonly ApiComposition legacyComposition;
        private readonly ApiEnvironmentId legacyEnvironmentId;

        [Obsolete("Legacy caller context only; not used for central discovery.")]
        public ApiComposition Composition => legacyComposition;

        [Obsolete("Legacy caller context only; not used for central discovery.")]
        public ApiEnvironmentId EnvironmentId => legacyEnvironmentId;

        [Obsolete("Legacy caller context only; not used for central discovery.")]
        public ApiEnvironmentStatus EnvironmentStatus =>
            legacyComposition?.GetEnvironmentStatus(legacyEnvironmentId);

        public Task<ApiResult<
            SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>>
            GetBuildVersionAsync(
                string buildVersion,
                string product,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return apiClient.SendAsync<SimultriaResourceResponse<
                    SimultriaUnityBuildVersionDto>>(
                SimultriaEndpointCatalog.UnityBuildVersion(
                    buildVersion,
                    product),
                cancellationToken);
        }
    }
}
