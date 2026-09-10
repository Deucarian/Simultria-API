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
