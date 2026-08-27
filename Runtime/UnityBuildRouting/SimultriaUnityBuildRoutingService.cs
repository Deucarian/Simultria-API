using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;

namespace Deucarian.Simultria.UnityBuildRouting
{
    /// <summary>
    /// Reusable product/version-to-environment routing backed exclusively by
    /// the Simultria Unity build directory. No environment fallback exists.
    /// </summary>
    public sealed class SimultriaUnityBuildRoutingService
    {
        private readonly IApiClient apiClient;
        private readonly ApiComposition composition;
        private readonly ApiEnvironmentId directoryEnvironment;

        public SimultriaUnityBuildRoutingService(
            IApiClient client,
            ApiComposition apiComposition,
            ApiEnvironmentId buildDirectoryEnvironment)
        {
            apiClient = client;
            composition = apiComposition ??
                throw new ArgumentNullException(nameof(apiComposition));
            directoryEnvironment = buildDirectoryEnvironment;
        }

        public async Task<SimultriaUnityBuildRoutingResult> ResolveAsync(
            string buildVersionValue,
            string productValue,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string buildVersion = (buildVersionValue ?? string.Empty).Trim();
            string product = (productValue ?? string.Empty).Trim();
            if (buildVersion.Length == 0)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_version_missing",
                    "Build routing requires a Unity build version.");
            }

            if (product.Length == 0)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_product_missing",
                    "Build routing requires a canonical Simultria product.");
            }

            if (directoryEnvironment.IsEmpty)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_environment_missing",
                    "Choose the configured API environment that hosts the " +
                    "Simultria Unity build directory.");
            }

            ApiEnvironmentStatus directoryStatus =
                composition.GetEnvironmentStatus(directoryEnvironment);
            if (!directoryStatus.IsResolved)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_environment_unavailable",
                    directoryStatus.Message);
            }

            if (apiClient == null)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_client_missing",
                    "An API client is required for build routing.");
            }

            ApiResult<SimultriaResourceResponse<SimultriaUnityBuildVersionDto>>
                lookup;
            try
            {
                lookup = await new SimultriaUnityBuildVersionLookupService(
                        apiClient,
                        composition,
                        directoryEnvironment)
                    .GetBuildVersionAsync(
                        buildVersion,
                        product,
                        cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_lookup_failed",
                    "The Simultria Unity build directory lookup failed (" +
                    exception.GetType().Name + ").");
            }

            if (lookup?.IsSuccess != true || lookup.Data?.Data == null)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_lookup_failed",
                    lookup?.HttpStatusCode.HasValue == true
                        ? "The Simultria Unity build directory rejected the " +
                          "lookup (HTTP " + lookup.HttpStatusCode.Value + ")."
                        : "The Simultria Unity build directory lookup did not " +
                          "return a usable response.");
            }

            SimultriaUnityBuildVersionDto response = lookup.Data.Data;
            return EvaluateResponse(buildVersion, product, response);
        }

        /// <summary>
        /// Validates a build-directory response obtained by a caller-specific
        /// transport. Editor build hooks can use a bounded synchronous HTTP
        /// transport without blocking UnityWebRequest's main-thread
        /// continuation, while sharing the same fail-closed routing policy.
        /// </summary>
        public SimultriaUnityBuildRoutingResult EvaluateResponse(
            string buildVersionValue,
            string productValue,
            SimultriaUnityBuildVersionDto response)
        {
            string buildVersion = (buildVersionValue ?? string.Empty).Trim();
            string product = (productValue ?? string.Empty).Trim();
            if (buildVersion.Length == 0)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_version_missing",
                    "Build routing requires a Unity build version.");
            }

            if (product.Length == 0)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_product_missing",
                    "Build routing requires a canonical Simultria product.");
            }

            if (response == null)
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_directory_lookup_failed",
                    "The Simultria Unity build directory did not return a " +
                    "usable response.");
            }

            if (!string.Equals(
                    response.Version?.Trim(),
                    buildVersion,
                    StringComparison.Ordinal))
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_version_mismatch",
                    "The build directory returned a different version. No " +
                    "fallback version was used.");
            }

            if (!string.Equals(
                    response.Product?.Trim(),
                    product,
                    StringComparison.Ordinal))
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_product_mismatch",
                    "The build directory returned a different product.");
            }

            if (!SimultriaBuildEnvironmentNameMapper.TryMap(
                    response.Environment,
                    out ApiEnvironmentId environment,
                    out string mappingError))
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_environment_unknown",
                    mappingError);
            }

            ApiEnvironmentStatus targetStatus =
                composition.GetEnvironmentStatus(environment);
            if (!targetStatus.IsResolved)
            {
                return Failure(
                    buildVersion,
                    product,
                    "resolved_environment_unavailable",
                    targetStatus.Message);
            }

            return SimultriaUnityBuildRoutingResult.Success(
                environment,
                buildVersion,
                product);
        }

        private static SimultriaUnityBuildRoutingResult Failure(
            string buildVersion,
            string product,
            string code,
            string message)
        {
            return SimultriaUnityBuildRoutingResult.Failure(
                buildVersion,
                product,
                code,
                message);
        }
    }
}
