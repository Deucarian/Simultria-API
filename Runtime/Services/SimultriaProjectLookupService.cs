using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>Read-only project and project-model lookup operations.</summary>
    public sealed class SimultriaProjectLookupService :
        SimultriaLookupServiceBase
    {
        public SimultriaProjectLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : base(apiClient, composition, environmentId)
        {
        }

        public Task<ApiResult<SimultriaCollectionResponse<SimultriaProjectDto>>>
            GetProjectsAsync(
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return SendAsync<
                SimultriaCollectionResponse<SimultriaProjectDto>>(
                SimultriaEndpointCatalog.Projects(
                    Composition,
                    EnvironmentId),
                cancellationToken);
        }

        public Task<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>>
            GetProjectAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return SendAsync<SimultriaResourceResponse<SimultriaProjectDto>>(
                SimultriaEndpointCatalog.Project(
                    Composition,
                    EnvironmentId,
                    projectId),
                cancellationToken);
        }

        public Task<ApiResult<SimultriaCollectionResponse<SimultriaModelDto>>>
            GetProjectModelsAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return SendAsync<
                SimultriaCollectionResponse<SimultriaModelDto>>(
                SimultriaEndpointCatalog.ProjectModels(
                    Composition,
                    EnvironmentId,
                    projectId),
                cancellationToken);
        }
    }
}
