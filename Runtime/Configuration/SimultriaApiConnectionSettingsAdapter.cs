using System;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>Validates project-owned connection settings for Simultria.</summary>
    public static class SimultriaApiConnectionSettingsAdapter
    {
        public static ApiComposition CreateComposition(
            ApiConnectionSettings settings)
        {
            if (!TryCreateComposition(settings, out ApiComposition composition,
                    out string message))
            {
                throw new InvalidOperationException(message);
            }

            return composition;
        }

        public static bool TryCreateComposition(
            ApiConnectionSettings settings,
            out ApiComposition composition,
            out string message)
        {
            composition = null;
            if (!IsCompatibleSettings(settings, out message))
            {
                return false;
            }

            return settings.TryCreateComposition(out composition, out message);
        }

        public static bool IsCompatibleSettings(
            ApiConnectionSettings settings,
            out string message)
        {
            if (settings == null)
            {
                message = "Assign the project's Simultria connection settings.";
                return false;
            }

            if (!IsCompatibleDefinition(settings.ServiceDefinition, out message))
            {
                return false;
            }

            return settings.TryValidate(out message);
        }

        public static bool IsCompatibleDefinition(
            ApiServiceDefinition definition,
            out string message)
        {
            if (definition == null)
            {
                message = "Assign the package-owned Simultria API definition.";
                return false;
            }

            if (!definition.TryGetId(out ApiServiceId serviceId) ||
                serviceId != SimultriaServiceIds.ApiV2)
            {
                message = "The service definition must use the stable ID '" +
                    SimultriaServiceIds.ApiV2.Value + "'.";
                return false;
            }

            if (!IsCompatibleCatalog(definition.EndpointCatalog, out message) ||
                !definition.TryGetEnvironmentDescriptors(
                    out var descriptors,
                    out message) ||
                !definition.TryGetRequiredClientIds(
                    out var clients,
                    out message))
            {
                return false;
            }

            foreach (ApiEnvironmentDescriptor expected in
                SimultriaEnvironmentDescriptors.Standard)
            {
                bool found = false;
                foreach (ApiEnvironmentDescriptor candidate in descriptors)
                {
                    if (candidate.EnvironmentId == expected.EnvironmentId &&
                        candidate.Stage == expected.Stage)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    message = "The Simultria definition is missing environment '" +
                        expected.EnvironmentId + "'.";
                    return false;
                }
            }

            bool hasPrimaryClient = false;
            foreach (ApiClientId client in clients)
            {
                if (client == SimultriaClientIds.Primary)
                {
                    hasPrimaryClient = true;
                    break;
                }
            }

            if (!hasPrimaryClient)
            {
                message = "The Simultria definition must declare client '" +
                    SimultriaClientIds.Primary.Value + "'.";
                return false;
            }

            message = null;
            return true;
        }

        public static bool IsCompatibleCatalog(
            ApiEndpointCatalog endpointCatalog,
            out string message)
        {
            if (endpointCatalog == null)
            {
                message = "Assign the Simultria API v2 endpoint catalog.";
                return false;
            }

            if (!string.Equals(
                    endpointCatalog.CatalogId,
                    SimultriaCatalogIds.ApiV2.Value,
                    StringComparison.Ordinal))
            {
                message =
                    "The endpoint catalog must use the stable Simultria API v2 ID.";
                return false;
            }

            if (!endpointCatalog.IsValid(out message))
            {
                return false;
            }

            foreach (ApiEndpointId endpointId in SimultriaEndpointIds.Stable)
            {
                if (!endpointCatalog.TryGetEndpoint(
                        endpointId,
                        out ApiEndpointCatalogEntry endpoint) ||
                    !string.Equals(
                        endpoint.ClientId,
                        SimultriaClientIds.Primary.Value,
                        StringComparison.Ordinal))
                {
                    message = "The Simultria catalog is missing compatible " +
                        "endpoint '" + endpointId + "'.";
                    return false;
                }
            }

            if (!HasSafeAuthentication(endpointCatalog, SimultriaEndpointIds.Login) ||
                !HasSafeAuthentication(
                    endpointCatalog,
                    SimultriaEndpointIds.ValidateAuthentication))
            {
                message = "Simultria authentication endpoints must explicitly " +
                    "declare authentication and suppress sensitive logging.";
                return false;
            }

            message = null;
            return true;
        }

        private static bool HasSafeAuthentication(
            ApiEndpointCatalog catalog,
            ApiEndpointId endpointId)
        {
            return catalog.TryGetEndpoint(
                    endpointId,
                    out ApiEndpointCatalogEntry endpoint) &&
                (endpoint.Authentication == ApiAuthenticationRequirement.Disabled ||
                 endpoint.Authentication == ApiAuthenticationRequirement.Required) &&
                endpoint.SuppressLogging;
        }
    }
}
