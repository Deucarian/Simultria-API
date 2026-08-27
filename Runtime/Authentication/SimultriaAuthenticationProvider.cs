using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Session;
using Deucarian.Authentication;

namespace Deucarian.Simultria.API.Authentication
{
    /// <summary>
    /// Concrete Simultria sign-in and validation semantics injected into the
    /// vendor-neutral Authentication lifecycle.
    /// </summary>
    public sealed class SimultriaAuthenticationProvider :
        IInteractiveAuthenticationAcquisitionProvider,
        IAuthenticationValidationProvider
    {
        private readonly AuthenticationEndpointProvider acquisition;
        private readonly AuthenticationEndpointValidationProvider
            validation;

        public SimultriaAuthenticationProvider(
            IApiClient apiClient,
            ApiComposition composition,
            ApiEnvironmentId environmentId)
        {
            if (apiClient == null)
            {
                throw new ArgumentNullException(nameof(apiClient));
            }

            Composition = composition ??
                throw new ArgumentNullException(nameof(composition));
            EnvironmentStatus = composition.GetEnvironmentStatus(environmentId);
            if (!EnvironmentStatus.IsResolved)
            {
                throw new InvalidOperationException(EnvironmentStatus.Message);
            }

            EnvironmentId = environmentId;
            acquisition = new AuthenticationEndpointProvider(
                apiClient,
                SimultriaAuthenticationConfiguration.CreateLogin(
                    composition,
                    environmentId));
            validation = new AuthenticationEndpointValidationProvider(
                apiClient,
                SimultriaAuthenticationConfiguration
                    .CreateValidation(composition, environmentId));
        }

        public ApiComposition Composition { get; }

        public ApiEnvironmentId EnvironmentId { get; }

        public ApiEnvironmentStatus EnvironmentStatus { get; }

        public string AcquisitionEndpoint => acquisition.EndpointTemplate;

        public string ValidationEndpoint => validation.EndpointTemplate;

        public IReadOnlyList<AuthenticationInputDescriptor>
            InputDescriptors => acquisition.InputDescriptors;

        string IAuthenticationAcquisitionProvider.DisplayName =>
            "Sign in to Simultria";

        string IAuthenticationValidationProvider.DisplayName =>
            "Simultria server validation";

        public Task<SessionResult> AcquireAsync(
            ISessionService sessionService,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return acquisition.AcquireAsync(
                sessionService,
                cancellationToken);
        }

        public Task<SessionResult> AcquireAsync(
            ISessionService sessionService,
            AuthenticationInputValues inputValues,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return acquisition.AcquireAsync(
                sessionService,
                inputValues,
                cancellationToken);
        }

        public Task<AuthenticationValidationResult> ValidateAsync(
            ISessionService sessionService,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return validation.ValidateAsync(
                sessionService,
                cancellationToken);
        }
    }
}
