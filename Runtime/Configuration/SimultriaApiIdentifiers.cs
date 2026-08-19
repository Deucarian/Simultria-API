using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>Stable environment IDs shipped by this integration.</summary>
    public static class SimultriaEnvironmentIds
    {
        public static readonly ApiEnvironmentId Development =
            new ApiEnvironmentId("simultria.development");

        /// <summary>
        /// Stable future-facing ID. No acceptance host is shipped until one is
        /// explicitly configured and verified.
        /// </summary>
        public static readonly ApiEnvironmentId Acceptance =
            new ApiEnvironmentId("simultria.acceptance");

        /// <summary>
        /// Stable future-facing ID. No production host is shipped until one is
        /// explicitly configured and verified.
        /// </summary>
        public static readonly ApiEnvironmentId Production =
            new ApiEnvironmentId("simultria.production");
    }

    /// <summary>Stable named clients used by the Simultria environment assets.</summary>
    public static class SimultriaClientIds
    {
        public static readonly ApiClientId Primary =
            new ApiClientId("simultria.primary");
    }

    /// <summary>Stable ID for the Simultria API v2 endpoint catalog.</summary>
    public static class SimultriaCatalogIds
    {
        public static readonly ApiCatalogId ApiV2 =
            new ApiCatalogId("simultria.api-v2");
    }

    /// <summary>Stable endpoint IDs independent of deployment URLs.</summary>
    public static class SimultriaEndpointIds
    {
        public static readonly ApiEndpointId Login =
            new ApiEndpointId("simultria.auth.login");
        public static readonly ApiEndpointId ValidateAuthentication =
            new ApiEndpointId("simultria.auth.validate");
        public static readonly ApiEndpointId Projects =
            new ApiEndpointId("simultria.projects.list");
        public static readonly ApiEndpointId Project =
            new ApiEndpointId("simultria.projects.get");
        public static readonly ApiEndpointId ProjectModels =
            new ApiEndpointId("simultria.project-models.list");
        public static readonly ApiEndpointId Model =
            new ApiEndpointId("simultria.models.get");
        public static readonly ApiEndpointId ModelVersion =
            new ApiEndpointId("simultria.model-versions.get");
        public static readonly ApiEndpointId ActiveModelVersion =
            new ApiEndpointId("simultria.model-versions.active");
        public static readonly ApiEndpointId FrozenModelVersion =
            new ApiEndpointId("simultria.model-versions.frozen");
        public static readonly ApiEndpointId ModelVersionDownload =
            new ApiEndpointId("simultria.model-versions.download");
        public static readonly ApiEndpointId ModelVersionActivities =
            new ApiEndpointId("simultria.activities.list");
        public static readonly ApiEndpointId ModelVersionActivity =
            new ApiEndpointId("simultria.activities.get");
    }
}
