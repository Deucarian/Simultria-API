using System;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Authoritative public Simultria build directories. Explicit lookup
    /// selection is independent of runtime backend profiles and overrides.
    /// </summary>
    public static class SimultriaUnityBuildDirectory
    {
        public const string BaseUrl = "https://buildingvirtualitysuite.com";

        public const string DevelopmentBaseUrl = "https://backend.dev-buildingvirtuality.com";

        public const string VersionRoute =
            "/api/v2/unity/builds/versions/{id}/{product}";

        internal static ApiEndpoint CreateEndpoint(string buildVersion,
                                                   string product,
                                                   SimultriaUnityBuildLookupEnvironment lookupEnvironment)
        {
            if (!TryGetBaseUrl(lookupEnvironment, out string baseUrl))
            {
                throw new ArgumentOutOfRangeException(nameof(lookupEnvironment),
                                                      "Select a supported Unity build lookup environment."
                );
            }

            if (string.IsNullOrWhiteSpace(buildVersion))
            {
                throw new ArgumentException("A build version is required.", nameof(buildVersion));
            }

            if (string.IsNullOrWhiteSpace(product))
            {
                throw new ArgumentException("A product is required.", nameof(product));
            }

            return new ApiEndpoint(baseUrl + VersionRoute,
                                   authentication: ApiAuthenticationRequirement.Disabled,
                                   timeoutSeconds: 30,
                                   suppressLogging: true
                )
                .WithPathParameter("id", buildVersion.Trim())
                .WithPathParameter("product", product.Trim());
        }

        public static bool TryGetBaseUrl(
            SimultriaUnityBuildLookupEnvironment lookupEnvironment,
            out string baseUrl)
        {
            switch (lookupEnvironment)
            {
                case SimultriaUnityBuildLookupEnvironment.Production:
                    baseUrl = BaseUrl;
                    return true;
                case SimultriaUnityBuildLookupEnvironment.Development:
                    baseUrl = DevelopmentBaseUrl;
                    return true;
                default:
                    baseUrl = string.Empty;
                    return false;
            }
        }
    }
}
