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
        private readonly SimultriaLookupContext _lookup;

        public SimultriaProjectLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : this(new SimultriaLookupContext(apiClient, composition, environmentId))
        {
        }

        public SimultriaProjectLookupService(SimultriaLookupContext context) : base(context)
        {
            _lookup = context;
        }

        public Task<ApiResult<SimultriaCollectionResponse<SimultriaProjectDto>>>
            GetProjectsAsync(
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<
                SimultriaCollectionResponse<SimultriaProjectDto>>(
                SimultriaEndpointCatalog.Projects(
                    _lookup.Composition,
                    _lookup.EnvironmentId),
                cancellationToken);
        }

        public Task<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>>
            GetProjectAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<SimultriaResourceResponse<SimultriaProjectDto>>(
                SimultriaEndpointCatalog.Project(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    projectId),
                cancellationToken);
        }

        public Task<ApiResult<SimultriaCollectionResponse<SimultriaModelDto>>>
            GetProjectModelsAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<
                SimultriaCollectionResponse<SimultriaModelDto>>(
                SimultriaEndpointCatalog.ProjectModels(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    projectId),
                cancellationToken);
        }
    }
}
