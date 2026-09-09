namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Authoritative public Simultria build directory. Runtime backend profiles
    /// and Editor environment overrides never change this discovery address.
    /// </summary>
    public static class SimultriaUnityBuildDirectory
    {
        public const string BaseUrl = "https://buildingvirtualitysuite.com";

        public const string VersionRoute =
            "/api/v2/unity/builds/versions/{id}/{product}";
    }
}
