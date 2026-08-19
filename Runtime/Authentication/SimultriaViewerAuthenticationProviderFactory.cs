using System;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;

namespace Deucarian.Simultria.API.Authentication
{
    /// <summary>Creates Simultria providers from resolved environment inputs.</summary>
    public static class SimultriaViewerAuthenticationProviderFactory
    {
        public static SimultriaViewerAuthenticationProvider Create(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            return new SimultriaViewerAuthenticationProvider(
                apiClient,
                composition,
                environmentId);
        }

        public static SimultriaViewerAuthenticationProvider Create(
            SimultriaApiProfile profile,
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return Create(
                profile.CreateComposition(),
                environmentId,
                apiClient);
        }

        public static SimultriaViewerAuthenticationProvider Create(
            ApiConnectionProfile profile,
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            return Create(
                SimultriaApiConnectionProfileAdapter.CreateComposition(profile),
                environmentId,
                apiClient);
        }

        public static SimultriaViewerAuthenticationProvider Create(
            ApiEnvironmentId environmentId,
            IApiClient apiClient)
        {
            if (!SimultriaApiProfileDefaults.TryLoad(
                    out SimultriaApiProfile profile))
            {
                throw new InvalidOperationException(
                    "The package-provided Simultria API profile is missing.");
            }

            return Create(profile, environmentId, apiClient);
        }

        public static bool TryCreate(
            SimultriaApiProfile profile,
            ApiEnvironmentId environmentId,
            IApiClient apiClient,
            out SimultriaViewerAuthenticationProvider provider,
            out ApiEnvironmentStatus status,
            out string message)
        {
            provider = null;
            status = null;
            if (profile == null)
            {
                message = "Assign a Simultria API profile.";
                return false;
            }

            if (!profile.TryCreateComposition(
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

        public static bool TryCreate(
            ApiConnectionProfile profile,
            ApiEnvironmentId environmentId,
            IApiClient apiClient,
            out SimultriaViewerAuthenticationProvider provider,
            out ApiEnvironmentStatus status,
            out string message)
        {
            provider = null;
            status = null;
            if (!SimultriaApiConnectionProfileAdapter.TryCreateComposition(
                    profile,
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
