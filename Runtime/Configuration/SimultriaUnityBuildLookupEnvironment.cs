namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Selects the public build directory, not the runtime backend assigned by
    /// its version record. Zero preserves Production for existing consumers.
    /// </summary>
    public enum SimultriaUnityBuildLookupEnvironment
    {
        Production = 0,
        Development = 1
    }
}
