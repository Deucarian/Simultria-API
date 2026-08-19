using System;
using Deucarian.API;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Session.APIIntegration;
using Deucarian.Simultria.API.Endpoints;

namespace Deucarian.Simultria.API.Authentication
{
    internal static class SimultriaAuthenticationConfiguration
    {
        internal static SessionTokenEndpointConfig CreateLogin(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            ApiEndpoint endpoint = SimultriaEndpointCatalog.Login(
                composition,
                environmentId);
            return new SessionTokenEndpointConfig(
                endpoint.Path,
                new[]
                {
                    new SessionTokenEndpointInputDefinition(
                        "identity",
                        "identity",
                        "Email / Username",
                        SessionTokenEndpointInputPlacement.JsonBody,
                        isSecret: false,
                        isRequired: true),
                    new SessionTokenEndpointInputDefinition(
                        "password",
                        "password",
                        "Password",
                        SessionTokenEndpointInputPlacement.JsonBody,
                        isSecret: true,
                        isRequired: true)
                },
                new SessionTokenEndpointResponseMapping(
                    accessTokenJsonPath: "access_token",
                    useJwtExpiryFallback: true),
                endpoint.Method,
                ResolveTimeoutSeconds(endpoint),
                ResolveBearerRequirement(endpoint));
        }

        internal static SessionTokenEndpointConfig CreateValidation(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            ApiEndpoint endpoint = SimultriaEndpointCatalog
                .ValidateAuthentication(composition, environmentId);
            return new SessionTokenEndpointConfig(
                endpoint.Path,
                null,
                new SessionTokenEndpointResponseMapping(
                    accessTokenJsonPath: "access_token",
                    useJwtExpiryFallback: true),
                endpoint.Method,
                ResolveTimeoutSeconds(endpoint),
                ResolveBearerRequirement(endpoint));
        }

        private static int ResolveTimeoutSeconds(ApiEndpoint endpoint)
        {
            if (endpoint.TimeoutSeconds.HasValue)
            {
                return endpoint.TimeoutSeconds.Value;
            }

            return endpoint.RequestPolicy != null
                ? endpoint.RequestPolicy.TimeoutSeconds
                : 0;
        }

        private static bool ResolveBearerRequirement(ApiEndpoint endpoint)
        {
            switch (endpoint.Authentication)
            {
                case ApiAuthenticationRequirement.Disabled:
                    return false;
                case ApiAuthenticationRequirement.Required:
                    return true;
                default:
                    throw new InvalidOperationException(
                        "Simultria token endpoints must resolve authentication " +
                        "to Disabled or Required before the session exchange is " +
                        "created.");
            }
        }
    }
}
