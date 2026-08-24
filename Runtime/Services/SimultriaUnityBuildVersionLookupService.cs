using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>
    /// Reads the public Simultria Unity build directory through an explicitly
    /// configured API environment. The returned environment is not defaulted.
    /// </summary>
    public sealed class SimultriaUnityBuildVersionLookupService :
        SimultriaLookupServiceBase
    {
        public SimultriaUnityBuildVersionLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId directoryEnvironmentId)
            : base(apiClient, composition, directoryEnvironmentId)
        {
        }

        public Task<ApiResult<
            SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>>
            GetBuildVersionAsync(
                string buildVersion,
                string product,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return SendAsync<SimultriaResourceResponse<
                    SimultriaUnityBuildVersionDto>>(
                SimultriaEndpointCatalog.UnityBuildVersion(
                    Composition,
                    EnvironmentId,
                    buildVersion,
                    product),
                cancellationToken);
        }
    }
}
