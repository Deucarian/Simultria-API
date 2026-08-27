using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;

namespace Deucarian.Simultria.API.Authentication
{
    /// <summary>Creates Simultria providers from explicit resolved inputs.</summary>
    public static class SimultriaAuthenticationProviderFactory
    {
        public static SimultriaAuthenticationProvider Create(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            return new SimultriaAuthenticationProvider(
                apiClient,
                composition,
                environmentId);
        }

        public static SimultriaAuthenticationProvider Create(
            ApiConnectionSettings settings,
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            return Create(
                SimultriaApiConnectionSettingsAdapter.CreateComposition(
                    settings),
                environmentId,
                apiClient);
        }

        public static bool TryCreate(
            ApiConnectionSettings settings,
            ApiEnvironmentId environmentId,
            IApiClient apiClient,
            out SimultriaAuthenticationProvider provider,
            out ApiEnvironmentStatus status,
            out string message)
        {
            provider = null;
            status = null;
            if (!SimultriaApiConnectionSettingsAdapter.TryCreateComposition(
                    settings,
                    out ApiComposition composition,
                    out message))
            {
                return false;
            }

            status = composition.GetEnvironmentStatus(environmentId);
            if (!status.IsResolved)
            {
                message = status.Message;
                return false;
            }

            if (apiClient == null)
            {
                message = "A Deucarian API client is required.";
                return false;
            }

            provider = Create(composition, environmentId, apiClient);
            message = null;
            return true;
        }
    }
}
