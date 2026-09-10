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
    /// Reusable product/version-to-environment routing using one explicitly
    /// selected directory (Production by default). Only explicit missing records
    /// are classified for an integration-owned build-profile fallback.
    /// </summary>
    public sealed class SimultriaUnityBuildRoutingService
    {
        private readonly IApiClient apiClient;
        private readonly ApiComposition composition;
        private readonly SimultriaUnityBuildLookupEnvironment lookupEnvironment;

        public SimultriaUnityBuildRoutingService(
            IApiClient client,
            ApiComposition targetComposition)
            : this(client, SimultriaUnityBuildLookupEnvironment.Production, targetComposition)
        {
        }

        public SimultriaUnityBuildRoutingService(
            IApiClient client,
            SimultriaUnityBuildLookupEnvironment lookupEnvironment,
            ApiComposition targetComposition)
        {
            apiClient = client;
            this.lookupEnvironment = lookupEnvironment;
            composition = targetComposition ??
                throw new ArgumentNullException(nameof(targetComposition));
        }

        [Obsolete("Runtime directory selection is ignored. Use (client, lookupEnvironment, targetComposition).")]
        public SimultriaUnityBuildRoutingService(
            IApiClient client,
            ApiComposition apiComposition,
            ApiEnvironmentId buildDirectoryEnvironment)
            : this(client, apiComposition)
        {
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

            if (!SimultriaUnityBuildDirectory.TryGetBaseUrl(lookupEnvironment, out _))
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_lookup_environment_invalid",
                    "Select a supported Unity build lookup environment.");
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
                        apiClient, lookupEnvironment)
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

            return EvaluateLookupResult(buildVersion, product, lookup);
        }

        /// <summary>
        /// Evaluates a full transport result, including explicit missing-record
        /// responses. A bare HTTP 404 never authorizes environment fallback.
        /// </summary>
        public SimultriaUnityBuildRoutingResult EvaluateLookupResult(
            string buildVersionValue,
            string productValue,
            ApiResult<SimultriaResourceResponse<SimultriaUnityBuildVersionDto>> lookup)
        {
            string buildVersion = (buildVersionValue ?? string.Empty).Trim();
            string product = (productValue ?? string.Empty).Trim();
            if (buildVersion.Length == 0 || product.Length == 0)
            {
                return EvaluateResponse(buildVersion, product, null);
            }

            if (SimultriaUnityBuildMissingRecordPolicy.IsExplicitlyMissing(
                    lookup, buildVersion, product))
            {
                return Failure(
                    buildVersion,
                    product,
                    "build_version_not_found",
                    "The central Simultria directory has no matching build " +
                    "record. A captured build-profile fallback may be used.");
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
