using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>Read-only model and model-version lookup operations.</summary>
    public sealed class SimultriaModelLookupService : SimultriaLookupServiceBase
    {
        private readonly SimultriaLookupContext _lookup;

        public SimultriaModelLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : this(new SimultriaLookupContext(apiClient, composition, environmentId))
        {
        }

        public SimultriaModelLookupService(SimultriaLookupContext context) : base(context)
        {
            _lookup = context;
        }

        public Task<ApiResult<SimultriaResourceResponse<SimultriaModelDto>>>
            GetModelAsync(
                int modelId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<SimultriaResourceResponse<SimultriaModelDto>>(
                SimultriaEndpointCatalog.Model(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    modelId),
                cancellationToken);
        }

        public Task<
                ApiResult<SimultriaResourceResponse<SimultriaModelVersionDto>>>
            GetModelVersionAsync(
                int versionId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.ModelVersion(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    versionId),
                cancellationToken);
        }

        public Task<
                ApiResult<SimultriaResourceResponse<SimultriaModelVersionDto>>>
            GetActiveModelVersionAsync(
                int modelId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.ActiveModelVersion(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    modelId),
                cancellationToken);
        }

        public Task<
                ApiResult<SimultriaResourceResponse<SimultriaModelVersionDto>>>
            GetFrozenModelVersionAsync(
                int modelId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.FrozenModelVersion(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    modelId),
                cancellationToken);
        }
    }
}
