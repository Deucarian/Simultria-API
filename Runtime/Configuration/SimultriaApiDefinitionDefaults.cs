using Deucarian.API.Configuration;
using UnityEngine;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>Loads the package-owned, credential-free Simultria contract.</summary>
    public static class SimultriaApiDefinitionDefaults
    {
        public const string ServiceDefinitionResourcePath =
            "Deucarian/Simultria/API/SimultriaApiV2Definition";

        public const string ServiceDefinitionAssetPath =
            "Packages/com.deucarian.simultria-api/Runtime/Resources/" +
            "Deucarian/Simultria/API/SimultriaApiV2Definition.asset";

        public const string EndpointCatalogResourcePath =
            "Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog";

        public const string EndpointCatalogAssetPath =
            "Packages/com.deucarian.simultria-api/Runtime/Resources/" +
            "Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog.asset";

        public static ApiServiceDefinition LoadServiceDefinition()
        {
            return Resources.Load<ApiServiceDefinition>(
                ServiceDefinitionResourcePath);
        }

        public static bool TryLoadServiceDefinition(
            out ApiServiceDefinition definition)
        {
            definition = LoadServiceDefinition();
            return definition != null;
        }

        public static ApiEndpointCatalog LoadEndpointCatalog()
        {
            return Resources.Load<ApiEndpointCatalog>(
                EndpointCatalogResourcePath);
        }
    }
}
