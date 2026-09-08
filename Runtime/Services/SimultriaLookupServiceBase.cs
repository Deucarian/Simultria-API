using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>Compatibility facade. New lookup behavior belongs to a composed SimultriaLookupContext.</summary>
    public abstract class SimultriaLookupServiceBase
    {
        private readonly SimultriaLookupContext context;

        protected SimultriaLookupServiceBase(
            IApiClient client,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : this(new SimultriaLookupContext(client, composition, environmentId))
        {
        }

        protected SimultriaLookupServiceBase(SimultriaLookupContext lookupContext)
        {
            context = lookupContext ?? throw new ArgumentNullException(nameof(lookupContext));
        }

        public ApiComposition Composition => context.Composition;

        public ApiEnvironmentId EnvironmentId => context.EnvironmentId;

        public ApiEnvironmentStatus EnvironmentStatus => context.EnvironmentStatus;

        protected Task<ApiResult<T>> SendAsync<T>(
            ApiEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            return context.SendAsync<T>(endpoint, cancellationToken);
        }
    }
}
