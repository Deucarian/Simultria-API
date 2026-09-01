using System;
using System.Collections.Generic;
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
        private readonly List<ApiEnvironmentProfile> environments =
            new List<ApiEnvironmentProfile>();

        internal SimultriaTestComposition()
        {
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

            Definition = ApiServiceDefinition.CreateTransient(
                SimultriaServiceIds.ApiV2.Value,
                "Simultria API v2",
                Catalog,
                SimultriaEnvironmentDescriptors.All,
                new[] { SimultriaClientIds.Primary },
                "test",
                "sha256:test");

            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.All)
            {
                ApiEnvironmentProfile environment =
                    ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
                environment.EnvironmentId = descriptor.EnvironmentId.Value;
                environment.DisplayName = descriptor.DisplayName;
                environment.Clients.Add(
                    new ApiNamedClientDefinition
                    {
                        ClientId = SimultriaClientIds.Primary.Value,
                        BaseUrl = descriptor.EnvironmentId ==
                            SimultriaEnvironmentIds.Development
                            ? "https://api.example.invalid"
                            : string.Empty
                    });
                environments.Add(environment);
            }

            Environment = environments.Find(environment =>
                environment.EnvironmentId ==
                    SimultriaEnvironmentIds.Development.Value);
            Settings = ApiConnectionSettings.CreateTransient(
                environments,
                Definition);
            Composition = Settings.CreateComposition();
        }

        internal ApiEnvironmentProfile Environment { get; }

        internal ApiEndpointCatalog Catalog { get; }

        internal ApiServiceDefinition Definition { get; }

        internal ApiConnectionSettings Settings { get; }

        internal ApiComposition Composition { get; }

        internal void ConfigureEnvironment(
            ApiEnvironmentId environmentId,
            string baseUrl)
        {
            foreach (ApiEnvironmentProfile environment in environments)
            {
                if (environment.TryGetId(out ApiEnvironmentId candidateId) &&
                    candidateId == environmentId)
                {
                    environment.Clients[0].BaseUrl = baseUrl;
                    return;
                }
            }

            throw new ArgumentException(
                "Unknown test environment: " + environmentId,
                nameof(environmentId));
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Settings);
            UnityEngine.Object.DestroyImmediate(Definition);
            UnityEngine.Object.DestroyImmediate(Catalog);
            foreach (ApiEnvironmentProfile environment in environments)
            {
                UnityEngine.Object.DestroyImmediate(environment);
            }
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
