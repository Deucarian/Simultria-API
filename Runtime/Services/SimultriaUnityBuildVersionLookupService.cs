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
        private readonly SimultriaLookupContext _lookup;

        public SimultriaUnityBuildVersionLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId directoryEnvironmentId)
            : this(new SimultriaLookupContext(apiClient, composition, directoryEnvironmentId))
        {
        }

        public SimultriaUnityBuildVersionLookupService(SimultriaLookupContext context) : base(context)
        {
            _lookup = context;
        }

        public Task<ApiResult<
            SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>>
            GetBuildVersionAsync(
                string buildVersion,
                string product,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<SimultriaResourceResponse<
                    SimultriaUnityBuildVersionDto>>(
                SimultriaEndpointCatalog.UnityBuildVersion(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    buildVersion,
                    product),
                cancellationToken);
        }
    }
}
