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
        public SimultriaModelLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : base(apiClient, composition, environmentId)
        {
        }

        public Task<ApiResult<SimultriaResourceResponse<SimultriaModelDto>>>
            GetModelAsync(
                int modelId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return SendAsync<SimultriaResourceResponse<SimultriaModelDto>>(
                SimultriaEndpointCatalog.Model(
                    Composition,
                    EnvironmentId,
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
            return SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.ModelVersion(
                    Composition,
                    EnvironmentId,
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
            return SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.ActiveModelVersion(
                    Composition,
                    EnvironmentId,
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
            return SendAsync<
                SimultriaResourceResponse<SimultriaModelVersionDto>>(
                SimultriaEndpointCatalog.FrozenModelVersion(
                    Composition,
                    EnvironmentId,
                    modelId),
                cancellationToken);
        }
    }
}
