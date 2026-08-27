using Deucarian.API.Models;

namespace Deucarian.Simultria.UnityBuildRouting
{
    /// <summary>Sanitized, reusable build-directory routing result.</summary>
    public sealed class SimultriaUnityBuildRoutingResult
    {
        private SimultriaUnityBuildRoutingResult(
            bool succeeded,
            ApiEnvironmentId environmentId,
            string buildVersion,
            string product,
            string errorCode,
            string message)
        {
            Succeeded = succeeded;
            EnvironmentId = environmentId;
            BuildVersion = buildVersion ?? string.Empty;
            Product = product ?? string.Empty;
            ErrorCode = errorCode ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public ApiEnvironmentId EnvironmentId { get; }
        public string BuildVersion { get; }
        public string Product { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        internal static SimultriaUnityBuildRoutingResult Success(
            ApiEnvironmentId environmentId,
            string buildVersion,
            string product)
        {
            return new SimultriaUnityBuildRoutingResult(
                true,
                environmentId,
                buildVersion,
                product,
                null,
                null);
        }

        internal static SimultriaUnityBuildRoutingResult Failure(
            string buildVersion,
            string product,
            string errorCode,
            string message)
        {
            return new SimultriaUnityBuildRoutingResult(
                false,
                default(ApiEnvironmentId),
                buildVersion,
                product,
                errorCode,
                message);
        }
    }
}
