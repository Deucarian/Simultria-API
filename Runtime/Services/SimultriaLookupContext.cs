using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Services
{
    public sealed class SimultriaLookupContext
    {
        private readonly IApiClient _client;

        public SimultriaLookupContext(IApiClient client, ApiComposition composition, ApiEnvironmentId environmentId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            Composition = composition ?? throw new ArgumentNullException(nameof(composition));
            EnvironmentStatus = composition.GetEnvironmentStatus(environmentId);
            if (!EnvironmentStatus.IsResolved) throw new InvalidOperationException(EnvironmentStatus.Message);
            EnvironmentId = environmentId;
        }

        public ApiComposition Composition { get; }
        public ApiEnvironmentId EnvironmentId { get; }
        public ApiEnvironmentStatus EnvironmentStatus { get; }

        public Task<ApiResult<T>> SendAsync<T>(ApiEndpoint endpoint, CancellationToken cancellationToken)
            => _client.SendAsync<T>(endpoint, cancellationToken);
    }
}
