using System;
using Deucarian.API;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    internal sealed class SimultriaTestComposition : IDisposable
    {
        internal SimultriaTestComposition()
        {
            Environment = ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
            Environment.EnvironmentId = SimultriaEnvironmentIds.Development.Value;
            Environment.DisplayName = "Simultria Development";
            Environment.Clients.Add(
                new ApiNamedClientDefinition
                {
                    ClientId = SimultriaClientIds.Primary.Value,
                    BaseUrl =
                        "https://api.example.invalid"
                });

            Catalog = ScriptableObject.CreateInstance<ApiEndpointCatalog>();
            Catalog.CatalogId = SimultriaCatalogIds.ApiV2.Value;
            Catalog.DisplayName = "Simultria API v2";
            Add(
                SimultriaEndpointIds.Login,
                "api/v2/login",
                HttpMethod.POST,
                ApiAuthenticationRequirement.Disabled,
                suppressLogging: true);
            Add(
                SimultriaEndpointIds.ValidateAuthentication,
                "api/v2/auth/validate",
                HttpMethod.GET,
                ApiAuthenticationRequirement.Required,
                suppressLogging: true);
            Add(SimultriaEndpointIds.Projects, "api/v2/projects");
            Add(SimultriaEndpointIds.Project, "api/v2/projects/{id}");
            Add(
                SimultriaEndpointIds.ProjectModels,
                "api/v2/projects/{project_id}/models");
            Add(SimultriaEndpointIds.Model, "api/v2/projects/models/{id}");
            Add(
                SimultriaEndpointIds.ModelVersion,
                "api/v2/projects/models/versions/{id}");
            Add(
                SimultriaEndpointIds.ActiveModelVersion,
                "api/v2/projects/models/{model_id}/versions/active");
            Add(
                SimultriaEndpointIds.FrozenModelVersion,
                "api/v2/projects/models/{model_id}/versions/frozen");
            Add(
                SimultriaEndpointIds.ModelVersionDownload,
                "api/v2/projects/models/versions/{version_id}/download");
            Add(
                SimultriaEndpointIds.ModelVersionActivities,
                "api/v2/projects/models/versions/{version_id}/activities");
            Add(
                SimultriaEndpointIds.ModelVersionActivity,
                "api/v2/projects/models/versions/{version_id}/activities/{id}");
            Add(
                SimultriaEndpointIds.UnityBuildVersion,
                "api/v2/unity/builds/versions/{id}/{product}",
                authentication: ApiAuthenticationRequirement.Disabled,
                suppressLogging: true);

            Profile = SimultriaApiProfile.CreateTransient(
                new[] { Environment },
                Catalog);
            Composition = Profile.CreateComposition();
        }

        internal ApiEnvironmentProfile Environment { get; }

        internal ApiEndpointCatalog Catalog { get; }

        internal SimultriaApiProfile Profile { get; }

        internal ApiComposition Composition { get; }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Profile);
            UnityEngine.Object.DestroyImmediate(Catalog);
            UnityEngine.Object.DestroyImmediate(Environment);
        }

        private void Add(
            ApiEndpointId endpointId,
            string route,
            HttpMethod method = HttpMethod.GET,
            ApiAuthenticationRequirement authentication =
                ApiAuthenticationRequirement.Required,
            bool suppressLogging = false)
        {
            Catalog.Endpoints.Add(
                new ApiEndpointCatalogEntry
                {
                    EndpointId = endpointId.Value,
                    ClientId = SimultriaClientIds.Primary.Value,
                    RouteTemplate = route,
                    Method = method,
                    Authentication = authentication,
                    SuppressLogging = suppressLogging
                });
        }
    }
}
