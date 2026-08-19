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
            return new SessionTokenEndpointConfig(
                SimultriaEndpointCatalog.Login(composition, environmentId).Path,
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
                HttpMethod.POST,
                timeoutSeconds: 30,
                useCurrentAccessTokenAsBearer: false);
        }

        internal static SessionTokenEndpointConfig CreateValidation(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            return new SessionTokenEndpointConfig(
                SimultriaEndpointCatalog
                    .ValidateAuthentication(composition, environmentId)
                    .Path,
                null,
                new SessionTokenEndpointResponseMapping(
                    accessTokenJsonPath: "access_token",
                    useJwtExpiryFallback: true),
                HttpMethod.GET,
                timeoutSeconds: 30,
                useCurrentAccessTokenAsBearer: true);
        }
    }
}
