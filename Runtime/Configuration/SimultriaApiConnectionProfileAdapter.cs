using System;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Validates and composes generic API connection profiles for Simultria.
    /// Existing SimultriaApiProfile assets remain supported for serialized
    /// compatibility; new authoring can use ApiConnectionProfile directly.
    /// </summary>
    public static class SimultriaApiConnectionProfileAdapter
    {
        public static ApiComposition CreateComposition(
            ApiConnectionProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!IsCompatibleProfile(
                    profile,
                    out string compatibilityMessage))
            {
                throw new InvalidOperationException(compatibilityMessage);
            }

            return profile.CreateComposition();
        }

        public static bool TryCreateComposition(
            ApiConnectionProfile profile,
            out ApiComposition composition,
            out string message)
        {
            composition = null;
            if (profile == null)
            {
                message = "Assign a Simultria API connection profile.";
                return false;
            }

            if (!IsCompatibleProfile(profile, out message))
            {
                return false;
            }

            return profile.TryCreateComposition(out composition, out message);
        }

        public static bool IsCompatibleProfile(
            ApiConnectionProfile profile,
            out string message)
        {
            if (profile == null)
            {
                message = "Assign a Simultria API connection profile.";
                return false;
            }

            if (!IsCompatibleCatalog(profile.EndpointCatalog, out message))
            {
                return false;
            }

            foreach (ApiEnvironmentDescriptor expected in
                SimultriaEnvironmentDescriptors.Standard)
            {
                if (!HasKnownDescriptor(profile, expected))
                {
                    message = "The API connection profile is missing the " +
                        "Simultria environment descriptor '" +
                        expected.EnvironmentId + "'.";
                    return false;
                }

                ApiEnvironmentProfile environment = FindEnvironment(
                    profile,
                    expected.EnvironmentId);
                if (environment == null)
                {
                    message = "The API connection profile is missing the " +
                        "Simultria environment slot '" +
                        expected.EnvironmentId + "'.";
                    return false;
                }
            }

            foreach (ApiEnvironmentProfile environment in profile.Environments)
            {
                if (environment == null ||
                    !environment.TryGetId(out ApiEnvironmentId environmentId))
                {
                    message = "The API connection profile contains an invalid " +
                        "environment slot.";
                    return false;
                }

                if (!environment.TryGetClient(
                        SimultriaClientIds.Primary,
                        out _))
                {
                    message = "Environment '" + environmentId +
                        "' must define the named client '" +
                        SimultriaClientIds.Primary.Value + "'.";
                    return false;
                }
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
                message = "The endpoint catalog must use the stable ID '" +
                    SimultriaCatalogIds.ApiV2.Value + "'.";
                return false;
            }

            if (!endpointCatalog.IsValid(out message))
            {
                message = "The Simultria endpoint catalog is invalid. " +
                    message;
                return false;
            }

            foreach (ApiEndpointId endpointId in SimultriaEndpointIds.Stable)
            {
                if (!endpointCatalog.TryGetEndpoint(
                        endpointId,
                        out ApiEndpointCatalogEntry endpoint))
                {
                    message = "The Simultria endpoint catalog is missing the " +
                        "required stable endpoint '" + endpointId + "'.";
                    return false;
                }

                if (!string.Equals(
                        endpoint.ClientId,
                        SimultriaClientIds.Primary.Value,
                        StringComparison.Ordinal))
                {
                    message = "Stable endpoint '" + endpointId +
                        "' must use the Simultria primary client.";
                    return false;
                }
            }

            if (!HasConcreteTokenAuthentication(
                    endpointCatalog,
                    SimultriaEndpointIds.Login) ||
                !HasConcreteTokenAuthentication(
                    endpointCatalog,
                    SimultriaEndpointIds.ValidateAuthentication))
            {
                message = "Simultria login and validation endpoints must " +
                    "explicitly require or disable bearer authentication.";
                return false;
            }

            if (!HasSensitiveLoggingSuppressed(
                    endpointCatalog,
                    SimultriaEndpointIds.Login) ||
                !HasSensitiveLoggingSuppressed(
                    endpointCatalog,
                    SimultriaEndpointIds.ValidateAuthentication))
            {
                message = "Simultria login and validation endpoints must " +
                    "suppress API request, response, and error logging.";
                return false;
            }

            message = null;
            return true;
        }

        private static bool HasConcreteTokenAuthentication(
            ApiEndpointCatalog endpointCatalog,
            ApiEndpointId endpointId)
        {
            return endpointCatalog.TryGetEndpoint(
                    endpointId,
                    out ApiEndpointCatalogEntry endpoint) &&
                (endpoint.Authentication ==
                    ApiAuthenticationRequirement.Disabled ||
                 endpoint.Authentication ==
                    ApiAuthenticationRequirement.Required);
        }

        private static bool HasSensitiveLoggingSuppressed(
            ApiEndpointCatalog endpointCatalog,
            ApiEndpointId endpointId)
        {
            return endpointCatalog.TryGetEndpoint(
                    endpointId,
                    out ApiEndpointCatalogEntry endpoint) &&
                endpoint.SuppressLogging;
        }

        private static bool HasKnownDescriptor(
            ApiConnectionProfile profile,
            ApiEnvironmentDescriptor expected)
        {
            foreach (ApiEnvironmentDescriptorDefinition definition in
                profile.KnownEnvironmentDefinitions)
            {
                if (definition != null &&
                    string.Equals(
                        definition.EnvironmentId,
                        expected.EnvironmentId.Value,
                        StringComparison.Ordinal) &&
                    definition.Stage == expected.Stage)
                {
                    return true;
                }
            }

            return false;
        }

        private static ApiEnvironmentProfile FindEnvironment(
            ApiConnectionProfile profile,
            ApiEnvironmentId environmentId)
        {
            foreach (ApiEnvironmentProfile environment in profile.Environments)
            {
                if (environment != null &&
                    environment.TryGetId(out ApiEnvironmentId candidate) &&
                    candidate == environmentId)
                {
                    return environment;
                }
            }

            return null;
        }
    }
}
