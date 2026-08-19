using System;
using System.Collections.Generic;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using UnityEngine;

namespace Deucarian.Simultria.API.Configuration
{
    /// <summary>
    /// Explicit Simultria composition asset. It references generic API
    /// environment profiles and the Simultria API v2 endpoint catalog without
    /// storing an active environment or any credential.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SimultriaApiProfile",
        menuName = "Deucarian/Simultria/API Profile")]
    public sealed class SimultriaApiProfile : ScriptableObject
    {
        [SerializeField] private List<ApiEnvironmentProfile> environments =
            new List<ApiEnvironmentProfile>();
        [SerializeField] private ApiEndpointCatalog endpointCatalog;

        public IReadOnlyList<ApiEnvironmentProfile> Environments =>
            environments;

        public ApiEndpointCatalog EndpointCatalog => endpointCatalog;

        public ApiComposition CreateComposition()
        {
            return new ApiComposition(environments, endpointCatalog);
        }

        public bool TryCreateComposition(
            out ApiComposition composition,
            out string message)
        {
            try
            {
                composition = CreateComposition();
                message = null;
                return true;
            }
            catch (Exception)
            {
                composition = null;
                message = "The Simultria API profile is incomplete or invalid.";
                return false;
            }
        }

        public bool TryGetEnvironmentStatus(
            ApiEnvironmentId environmentId,
            out ApiEnvironmentStatus status,
            out string message)
        {
            if (!TryCreateComposition(out ApiComposition composition, out message))
            {
                status = null;
                return false;
            }

            status = composition.GetEnvironmentStatus(environmentId);
            message = status.Message;
            return status.IsResolved;
        }

        /// <summary>Creates an unsaved profile for composition and tests.</summary>
        public static SimultriaApiProfile CreateTransient(
            IEnumerable<ApiEnvironmentProfile> environmentProfiles,
            ApiEndpointCatalog catalog)
        {
            var profile = CreateInstance<SimultriaApiProfile>();
            profile.environments.Clear();
            if (environmentProfiles != null)
            {
                profile.environments.AddRange(environmentProfiles);
            }

            profile.endpointCatalog = catalog;
            return profile;
        }
    }
}
