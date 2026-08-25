namespace Deucarian.Simultria.API.Models
{
    /// <summary>Stable error codes returned by viewer model resolution.</summary>
    public static class SimultriaViewerModelErrorCodes
    {
        public const string InvalidProjectId = "invalid_project_id";
        public const string InvalidModelId = "invalid_model_id";
        public const string InvalidModelVersionId = "invalid_model_version_id";
        public const string ProjectRequestCanceled = "project_request_canceled";
        public const string ProjectRequestFailed = "project_request_failed";
        public const string ProjectNotFound = "project_not_found";
        public const string ModelNotFound = "model_not_found";
        public const string ModelVersionsMissing = "model_versions_missing";
        public const string ModelVersionNotFound = "model_version_not_found";
        public const string ModelDownloadUrlMissing =
            "model_download_url_missing";
    }

    /// <summary>Sanitized result of resolving viewer model content.</summary>
    public sealed class SimultriaViewerModelResolveResult
    {
        private SimultriaViewerModelResolveResult(
            bool succeeded,
            int projectId,
            string projectName,
            int modelId,
            string modelName,
            int modelVersionId,
            string modelVersionName,
            string downloadUrl,
            bool usedRequestedVersion,
            bool usedActiveVersion,
            string message,
            string errorCode)
        {
            Succeeded = succeeded;
            ProjectId = projectId;
            ProjectName = projectName;
            ModelId = modelId;
            ModelName = modelName;
            ModelVersionId = modelVersionId;
            ModelVersionName = modelVersionName;
            DownloadUrl = downloadUrl;
            UsedRequestedVersion = usedRequestedVersion;
            UsedActiveVersion = usedActiveVersion;
            Message = message;
            ErrorCode = errorCode;
        }

        public bool Succeeded { get; }

        public int ProjectId { get; }

        public string ProjectName { get; }

        public int ModelId { get; }

        public string ModelName { get; }

        public int ModelVersionId { get; }

        public string ModelVersionName { get; }

        public string DownloadUrl { get; }

        public bool UsedRequestedVersion { get; }

        /// <summary>
        /// Whether an unpinned request resolved the model's active version.
        /// </summary>
        public bool UsedActiveVersion { get; }

        public string Message { get; }

        public string ErrorCode { get; }

        internal static SimultriaViewerModelResolveResult Success(
            SimultriaProjectDto project,
            SimultriaModelDto model,
            SimultriaModelVersionDto version,
            bool usedRequestedVersion)
        {
            return Success(
                project,
                model,
                version,
                usedRequestedVersion,
                false);
        }

        internal static SimultriaViewerModelResolveResult Success(
            SimultriaProjectDto project,
            SimultriaModelDto model,
            SimultriaModelVersionDto version,
            bool usedRequestedVersion,
            bool usedActiveVersion)
        {
            return new SimultriaViewerModelResolveResult(
                true,
                project.Id,
                project.Name,
                model.Id,
                model.Name,
                version.Id,
                version.Name,
                version.DownloadUrl,
                usedRequestedVersion,
                usedActiveVersion,
                usedRequestedVersion
                    ? "Resolved the requested Simultria model version."
                    : usedActiveVersion
                        ? "Resolved the active Simultria model version."
                        : "Resolved the latest Simultria model version.",
                null);
        }

        internal static SimultriaViewerModelResolveResult Failure(
            string errorCode,
            string message)
        {
            return new SimultriaViewerModelResolveResult(
                false,
                0,
                null,
                0,
                null,
                0,
                null,
                null,
                false,
                false,
                message,
                errorCode);
        }
    }
}
