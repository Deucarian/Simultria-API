using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>
    /// Reads an explicitly selected public directory without a session or a
    /// runtime profile. Production is the default; the returned environment is not.
    /// </summary>
    public sealed class SimultriaUnityBuildVersionLookupService
    {
        private readonly Func<ApiEndpoint, CancellationToken, Task<ApiResult<
            SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>>> sendAsync;
        private readonly SimultriaUnityBuildLookupEnvironment lookupEnvironment;

        public SimultriaUnityBuildVersionLookupService(IApiClient apiClient)
            : this(apiClient, SimultriaUnityBuildLookupEnvironment.Production)
        {
        }

        public SimultriaUnityBuildVersionLookupService(
            IApiClient apiClient,
            SimultriaUnityBuildLookupEnvironment lookupEnvironment)
        {
            if (apiClient == null) throw new ArgumentNullException(nameof(apiClient));
            if (!SimultriaUnityBuildDirectory.TryGetBaseUrl(lookupEnvironment, out _))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lookupEnvironment), "Select a supported Unity build lookup environment.");
            }

            this.lookupEnvironment = lookupEnvironment;
            sendAsync = apiClient.SendAsync<
                SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>;
        }

        [Obsolete("Runtime directory selection is ignored. Use (apiClient, lookupEnvironment).")]
        public SimultriaUnityBuildVersionLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId directoryEnvironmentId)
            : this(apiClient)
        {
            legacyComposition = composition;
            legacyEnvironmentId = directoryEnvironmentId;
        }

        /// <summary>
        /// Adapts an existing context's transport only. Runtime composition and
        /// environment selection never participate in central discovery.
        /// </summary>
        [Obsolete("The build directory needs no runtime context. Use (apiClient, lookupEnvironment).")]
        public SimultriaUnityBuildVersionLookupService(SimultriaLookupContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            lookupEnvironment = SimultriaUnityBuildLookupEnvironment.Production;
            sendAsync = context.SendAsync<
                SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>;
            legacyComposition = context.Composition;
            legacyEnvironmentId = context.EnvironmentId;
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
            return sendAsync(
                SimultriaEndpointCatalog.UnityBuildVersion(
                    buildVersion,
                    product,
                    lookupEnvironment),
                cancellationToken);
        }
    }
}
