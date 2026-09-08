using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>Read-only activity metadata lookup operations.</summary>
    public sealed class SimultriaActivityLookupService :
        SimultriaLookupServiceBase
    {
        private readonly SimultriaLookupContext _lookup;

        public SimultriaActivityLookupService(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : this(new SimultriaLookupContext(apiClient, composition, environmentId))
        {
        }

        public SimultriaActivityLookupService(SimultriaLookupContext context) : base(context)
        {
            _lookup = context;
        }

        public Task<ApiResult<SimultriaCollectionResponse<SimultriaActivityDto>>>
            GetActivitiesAsync(
                int versionId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return GetActivitiesAsync<SimultriaActivityDto>(
                versionId,
                cancellationToken);
        }

        /// <summary>
        /// Loads activities into an integration-owned DTO. This lets Report
        /// retain issue/media fields without owning Simultria route logic.
        /// </summary>
        public Task<ApiResult<SimultriaCollectionResponse<TActivity>>>
            GetActivitiesAsync<TActivity>(
                int versionId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<SimultriaCollectionResponse<TActivity>>(
                SimultriaEndpointCatalog.ModelVersionActivities(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    versionId),
                cancellationToken);
        }

        public Task<ApiResult<SimultriaResourceResponse<SimultriaActivityDto>>>
            GetActivityAsync(
                int versionId,
                int activityId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return GetActivityAsync<SimultriaActivityDto>(
                versionId,
                activityId,
                cancellationToken);
        }

        public Task<ApiResult<SimultriaResourceResponse<TActivity>>>
            GetActivityAsync<TActivity>(
                int versionId,
                int activityId,
                CancellationToken cancellationToken =
                    default(CancellationToken))
        {
            return _lookup.SendAsync<SimultriaResourceResponse<TActivity>>(
                SimultriaEndpointCatalog.ModelVersionActivity(
                    _lookup.Composition,
                    _lookup.EnvironmentId,
                    versionId,
                    activityId),
                cancellationToken);
        }
    }
}
