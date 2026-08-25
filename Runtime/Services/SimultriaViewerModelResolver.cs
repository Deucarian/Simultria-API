using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Endpoints;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    /// <summary>
    /// Resolves a project/model/version selection to the documented download
    /// URL using exact, active, then deterministic latest-version precedence.
    /// </summary>
    public sealed class SimultriaViewerModelResolver :
        SimultriaLookupServiceBase
    {
        private static readonly string IncludedFields = string.Join(
            ",",
            new[]
            {
                "projects.id",
                "projects.name",
                "projects.models",
                "projects.sub_projects",
                "models.id",
                "models.name",
                "models.active_version",
                "models.model_versions",
                "models.versions",
                "model_versions.id",
                "model_versions.name",
                "model_versions.download_link",
                "model_versions.version",
                "model_versions.version_number",
                "model_versions.order",
                "model_versions.updated_at",
                "model_versions.created_at"
            });

        private readonly IApiClient apiClient;

        public SimultriaViewerModelResolver(
            IApiClient client,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
            : base(client, composition, environmentId)
        {
            apiClient = client;
        }

        public async Task<SimultriaViewerModelResolveResult> ResolveAsync(
            int projectId,
            int modelId,
            int? modelVersionId = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (projectId <= 0)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.InvalidProjectId,
                    "A positive Simultria project ID is required.");
            }

            if (modelId <= 0)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.InvalidModelId,
                    "A positive Simultria model ID is required.");
            }

            if (modelVersionId.HasValue && modelVersionId.Value <= 0)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.InvalidModelVersionId,
                    "The Simultria model version ID must be positive.");
            }

            ApiEndpoint endpoint = SimultriaEndpointCatalog.Project(
                    Composition,
                    EnvironmentId,
                    projectId)
                .WithQueryParameter("included_fields", IncludedFields);

            ApiResult<SimultriaResourceResponse<SimultriaProjectDto>> response;
            try
            {
                response = await apiClient.SendAsync<
                    SimultriaResourceResponse<SimultriaProjectDto>>(
                    endpoint,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ProjectRequestCanceled,
                    "The Simultria project request was canceled.");
            }
            catch (Exception)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ProjectRequestFailed,
                    "The Simultria project request failed.");
            }

            if (response == null || !response.IsSuccess)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ProjectRequestFailed,
                    CreateRequestFailureMessage(response));
            }

            if (response.Data?.Data == null)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ProjectNotFound,
                    "The Simultria project response contained no project.");
            }

            return ResolveFromProjects(
                projectId,
                modelId,
                modelVersionId,
                new[] { response.Data.Data });
        }

        public static SimultriaViewerModelResolveResult ResolveFromProjects(
            int projectId,
            int modelId,
            int? modelVersionId,
            IEnumerable<SimultriaProjectDto> projects)
        {
            SimultriaProjectDto project = FindProject(projects, projectId);
            if (project == null)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ProjectNotFound,
                    "The requested Simultria project was not found.");
            }

            SimultriaModelDto model = FindModel(project.Models, modelId);
            if (model == null)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ModelNotFound,
                    "The requested Simultria model was not found in the project.");
            }

            List<SimultriaModelVersionDto> versions = GetCandidateVersions(model);
            if (versions.Count == 0)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ModelVersionsMissing,
                    "The Simultria model response contained no model versions.");
            }

            bool exactVersionRequested = modelVersionId.HasValue;
            bool activeVersionSelected =
                !exactVersionRequested && model.ActiveVersion?.Id > 0;
            SimultriaModelVersionDto version;
            if (exactVersionRequested)
            {
                version = FindVersion(versions, modelVersionId.Value);
            }
            else if (activeVersionSelected)
            {
                version = SelectActiveVersion(model);
            }
            else
            {
                version = SelectLatestVersion(versions);
            }
            if (version == null)
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ModelVersionNotFound,
                    activeVersionSelected
                        ? "The active Simultria model version was not returned " +
                          "with the model's version details."
                        : "The requested Simultria model version was not found.");
            }

            if (string.IsNullOrWhiteSpace(version.DownloadUrl))
            {
                return SimultriaViewerModelResolveResult.Failure(
                    SimultriaViewerModelErrorCodes.ModelDownloadUrlMissing,
                    "The selected Simultria model version has no download URL.");
            }

            return SimultriaViewerModelResolveResult.Success(
                project,
                model,
                version,
                exactVersionRequested,
                activeVersionSelected);
        }

        public static SimultriaModelVersionDto SelectLatestVersion(
            IEnumerable<SimultriaModelVersionDto> versions)
        {
            SimultriaModelVersionDto selected = null;
            if (versions == null)
            {
                return null;
            }

            foreach (SimultriaModelVersionDto candidate in versions)
            {
                if (candidate != null &&
                    (selected == null || CompareVersions(candidate, selected) > 0))
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        private static int CompareVersions(
            SimultriaModelVersionDto left,
            SimultriaModelVersionDto right)
        {
            int comparison = CompareNullable(left.OrderValue, right.OrderValue);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareNullable(
                left.VersionNumberValue,
                right.VersionNumberValue);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareSemanticVersions(
                ParseSemanticVersion(left.Version),
                ParseSemanticVersion(right.Version));
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareNullable(
                left.UpdatedAtUtc,
                right.UpdatedAtUtc);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareNullable(
                left.CreatedAtUtc,
                right.CreatedAtUtc);
            return comparison != 0 ? comparison : left.Id.CompareTo(right.Id);
        }

        private static int CompareNullable<T>(T? left, T? right)
            where T : struct, IComparable<T>
        {
            if (!left.HasValue)
            {
                return right.HasValue ? -1 : 0;
            }

            return !right.HasValue
                ? 1
                : left.Value.CompareTo(right.Value);
        }

        private static int[] ParseSemanticVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string[] parts = value.Trim().Split('.');
            var parsed = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(
                        parts[i],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out parsed[i]))
                {
                    return null;
                }
            }

            return parsed;
        }

        private static int CompareSemanticVersions(int[] left, int[] right)
        {
            if (left == null)
            {
                return right == null ? 0 : -1;
            }

            if (right == null)
            {
                return 1;
            }

            int count = Math.Max(left.Length, right.Length);
            for (int i = 0; i < count; i++)
            {
                int leftValue = i < left.Length ? left[i] : 0;
                int rightValue = i < right.Length ? right[i] : 0;
                int comparison = leftValue.CompareTo(rightValue);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static SimultriaProjectDto FindProject(
            IEnumerable<SimultriaProjectDto> projects,
            int projectId)
        {
            if (projects == null)
            {
                return null;
            }

            foreach (SimultriaProjectDto project in projects)
            {
                if (project == null)
                {
                    continue;
                }

                if (project.Id == projectId)
                {
                    return project;
                }

                SimultriaProjectDto nested = FindProject(
                    project.SubProjects,
                    projectId);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static SimultriaModelDto FindModel(
            IEnumerable<SimultriaModelDto> models,
            int modelId)
        {
            if (models == null)
            {
                return null;
            }

            foreach (SimultriaModelDto model in models)
            {
                if (model != null && model.Id == modelId)
                {
                    return model;
                }
            }

            return null;
        }

        private static SimultriaModelVersionDto FindVersion(
            IEnumerable<SimultriaModelVersionDto> versions,
            int versionId)
        {
            if (versions == null)
            {
                return null;
            }

            foreach (SimultriaModelVersionDto version in versions)
            {
                if (version != null && version.Id == versionId)
                {
                    return version;
                }
            }

            return null;
        }

        private static SimultriaModelVersionDto SelectActiveVersion(
            SimultriaModelDto model)
        {
            SimultriaModelVersionDto active = model?.ActiveVersion;
            if (active?.Id <= 0)
            {
                return null;
            }

            SimultriaModelVersionDto detailed =
                FindVersion(model.ModelVersions, active.Id) ??
                FindVersion(model.Versions, active.Id);
            if (detailed != null &&
                !string.IsNullOrWhiteSpace(detailed.DownloadUrl))
            {
                return detailed;
            }

            return !string.IsNullOrWhiteSpace(active.DownloadUrl)
                ? active
                : detailed;
        }

        private static List<SimultriaModelVersionDto> GetCandidateVersions(
            SimultriaModelDto model)
        {
            var versions = new List<SimultriaModelVersionDto>();
            var ids = new HashSet<int>();
            AddVersions(versions, ids, model?.ModelVersions);
            AddVersions(versions, ids, model?.Versions);
            AddVersions(
                versions,
                ids,
                model?.ActiveVersion == null
                    ? null
                    : new[] { model.ActiveVersion });
            return versions;
        }

        private static void AddVersions(
            ICollection<SimultriaModelVersionDto> destination,
            ISet<int> ids,
            IEnumerable<SimultriaModelVersionDto> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (SimultriaModelVersionDto version in source)
            {
                if (version != null && ids.Add(version.Id))
                {
                    destination.Add(version);
                }
            }
        }

        private static string CreateRequestFailureMessage(
            ApiResult<SimultriaResourceResponse<SimultriaProjectDto>> response)
        {
            if (response?.Error?.HttpStatusCode.HasValue == true)
            {
                return "The Simultria project request failed with HTTP " +
                       response.Error.HttpStatusCode.Value + ".";
            }

            return "The Simultria project request failed.";
        }
    }
}
