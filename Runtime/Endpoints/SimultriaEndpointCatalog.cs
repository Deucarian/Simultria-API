using System;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;

namespace Deucarian.Simultria.API.Endpoints
{
    /// <summary>
    /// Typed accessors over the package-provided API v2 catalog. Route and host
    /// ownership remain in API composition assets.
    /// </summary>
    public static class SimultriaEndpointCatalog
    {
        public static ApiEndpoint Login(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            return Resolve(
                composition,
                environmentId,
                SimultriaEndpointIds.Login);
        }

        public static ApiEndpoint ValidateAuthentication(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            return Resolve(
                composition,
                environmentId,
                SimultriaEndpointIds.ValidateAuthentication);
        }

        public static ApiEndpoint Projects(
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            return Resolve(
                composition,
                environmentId,
                SimultriaEndpointIds.Projects);
        }

        public static ApiEndpoint Project(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int projectId)
        {
            return Resolve(composition, environmentId, SimultriaEndpointIds.Project)
                .WithPathParameter("id", RequireId(projectId, nameof(projectId)));
        }

        public static ApiEndpoint ProjectModels(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int projectId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ProjectModels)
                .WithPathParameter(
                    "project_id",
                    RequireId(projectId, nameof(projectId)));
        }

        public static ApiEndpoint Model(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int modelId)
        {
            return Resolve(composition, environmentId, SimultriaEndpointIds.Model)
                .WithPathParameter("id", RequireId(modelId, nameof(modelId)));
        }

        public static ApiEndpoint ModelVersion(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int versionId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ModelVersion)
                .WithPathParameter("id", RequireId(versionId, nameof(versionId)));
        }

        public static ApiEndpoint ActiveModelVersion(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int modelId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ActiveModelVersion)
                .WithPathParameter(
                    "model_id",
                    RequireId(modelId, nameof(modelId)));
        }

        public static ApiEndpoint FrozenModelVersion(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int modelId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.FrozenModelVersion)
                .WithPathParameter(
                    "model_id",
                    RequireId(modelId, nameof(modelId)));
        }

        public static ApiEndpoint ModelVersionDownload(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int versionId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ModelVersionDownload)
                .WithPathParameter(
                    "version_id",
                    RequireId(versionId, nameof(versionId)));
        }

        public static ApiEndpoint ModelVersionActivities(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int versionId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ModelVersionActivities)
                .WithPathParameter(
                    "version_id",
                    RequireId(versionId, nameof(versionId)));
        }

        public static ApiEndpoint ModelVersionActivity(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            int versionId,
            int activityId)
        {
            return Resolve(
                    composition,
                    environmentId,
                    SimultriaEndpointIds.ModelVersionActivity)
                .WithPathParameter(
                    "version_id",
                    RequireId(versionId, nameof(versionId)))
                .WithPathParameter(
                    "id",
                    RequireId(activityId, nameof(activityId)));
        }

        /// <summary>
        /// Resolves the public build-directory route used to discover the
        /// backend-selected environment for one Unity build.
        /// </summary>
        public static ApiEndpoint UnityBuildVersion(
            ApiComposition composition,
            ApiEnvironmentId directoryEnvironmentId,
            string buildVersion,
            string product)
        {
            return Resolve(
                    composition,
                    directoryEnvironmentId,
                    SimultriaEndpointIds.UnityBuildVersion)
                .WithPathParameter(
                    "id",
                    RequireSegment(buildVersion, nameof(buildVersion)))
                .WithPathParameter(
                    "product",
                    RequireSegment(product, nameof(product)));
        }

        private static ApiEndpoint Resolve(
            ApiComposition composition,
            ApiEnvironmentId environmentId,
            ApiEndpointId endpointId)
        {
            if (composition == null)
            {
                throw new ArgumentNullException(nameof(composition));
            }

            return composition.ResolveEndpoint(environmentId, endpointId).Endpoint;
        }

        private static int RequireId(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A positive Simultria resource ID is required.");
            }

            return value;
        }

        private static string RequireSegment(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A non-empty Simultria route value is required.",
                    parameterName);
            }

            return value.Trim();
        }
    }
}
