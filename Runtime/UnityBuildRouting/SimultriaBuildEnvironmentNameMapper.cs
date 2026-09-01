using System;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;

namespace Deucarian.Simultria.UnityBuildRouting
{
    /// <summary>
    /// Maps the environment names returned by the Simultria Unity build
    /// directory to canonical API environment identifiers.
    /// </summary>
    public static class SimultriaBuildEnvironmentNameMapper
    {
        public static bool TryMap(
            string environmentName,
            out ApiEnvironmentId environmentId,
            out string error)
        {
            environmentId = default(ApiEnvironmentId);
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                error = "The Unity build response contains no environment.";
                return false;
            }

            switch (environmentName.Trim().ToLowerInvariant())
            {
                case "local":
                    environmentId = SimultriaEnvironmentIds.Local;
                    break;
                case "development":
                    environmentId = SimultriaEnvironmentIds.Development;
                    break;
                case "test":
                case "testing":
                    environmentId = SimultriaEnvironmentIds.Testing;
                    break;
                case "accept":
                case "acceptance":
                    environmentId = SimultriaEnvironmentIds.Acceptance;
                    break;
                case "production":
                    environmentId = SimultriaEnvironmentIds.Production;
                    break;
                default:
                    error = "The Unity build response contains an unknown " +
                        "environment '" + environmentName.Trim() + "'.";
                    return false;
            }

            error = null;
            return true;
        }
    }
}
