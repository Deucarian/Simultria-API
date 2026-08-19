using Deucarian.API.Configuration;
using Deucarian.API.Core;
using UnityEngine;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>Loads the package-provided, credential-free Simultria profile.</summary>
    public static class SimultriaApiProfileDefaults
    {
        public const string DefaultProfileResourcePath =
            "Deucarian/Simultria/API/SimultriaApiProfile";

        public const string DefaultProfileAssetPath =
            "Packages/com.deucarian.simultria-api/Runtime/Resources/" +
            "Deucarian/Simultria/API/SimultriaApiProfile.asset";

        public const string DefaultEndpointCatalogResourcePath =
            "Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog";

        public const string DefaultEndpointCatalogAssetPath =
            "Packages/com.deucarian.simultria-api/Runtime/Resources/" +
            "Deucarian/Simultria/API/SimultriaApiV2EndpointCatalog.asset";

        public static SimultriaApiProfile Load()
        {
            return Resources.Load<SimultriaApiProfile>(
                DefaultProfileResourcePath);
        }

        public static bool TryLoad(out SimultriaApiProfile profile)
        {
            profile = Load();
            return profile != null;
        }

        public static ApiEndpointCatalog LoadEndpointCatalog()
        {
            return Resources.Load<ApiEndpointCatalog>(
                DefaultEndpointCatalogResourcePath);
        }

        public static bool TryLoadEndpointCatalog(
            out ApiEndpointCatalog endpointCatalog)
        {
            endpointCatalog = LoadEndpointCatalog();
            return endpointCatalog != null;
        }

        public static bool TryCreateComposition(
            out ApiComposition composition,
            out string message)
        {
            if (!TryLoad(out SimultriaApiProfile profile))
            {
                composition = null;
                message = "The package-provided Simultria API profile is missing.";
                return false;
            }

            return profile.TryCreateComposition(out composition, out message);
        }
    }
}
