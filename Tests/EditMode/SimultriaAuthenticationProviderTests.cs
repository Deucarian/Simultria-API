using System;
using Deucarian.API;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Session.APIIntegration;
using Deucarian.Simultria.API.Authentication;
using Deucarian.Simultria.API.Configuration;
using Deucarian.Authentication;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaAuthenticationProviderTests
    {
        [Test]
        public void ProviderOwnsOnlySimultriaEndpointAndInputSemantics()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var provider = new SimultriaAuthenticationProvider(
                    new ApiClientSpy(),
                    fixture.Composition,
                    SimultriaEnvironmentIds.Development);

                Assert.That(provider.EnvironmentId,
                    Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(provider.EnvironmentStatus.IsResolved, Is.True);
                Assert.That(provider.AcquisitionEndpoint,
                    Does.EndWith("/api/v2/login"));
                Assert.That(provider.ValidationEndpoint,
                    Does.EndWith("/api/v2/auth/validate"));
                Assert.That(provider.InputDescriptors, Has.Count.EqualTo(2));
                Assert.That(provider.InputDescriptors[0].Key,
                    Is.EqualTo("identity"));
                Assert.That(provider.InputDescriptors[0].IsSecret, Is.False);
                Assert.That(provider.InputDescriptors[1].Key,
                    Is.EqualTo("password"));
                Assert.That(provider.InputDescriptors[1].IsSecret, Is.True);
                Assert.That(provider,
                    Is.InstanceOf<IInteractiveAuthenticationAcquisitionProvider>());
                Assert.That(provider,
                    Is.InstanceOf<IAuthenticationValidationProvider>());
            }
        }

        [Test]
        public void FactoryResolvesIdWithoutAConsumerOwnedUrl()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                bool created = SimultriaAuthenticationProviderFactory
                    .TryCreate(
                        fixture.Settings,
                        SimultriaEnvironmentIds.Development,
                        new ApiClientSpy(),
                        out SimultriaAuthenticationProvider provider,
                        out Deucarian.API.Core.ApiEnvironmentStatus status,
                        out string message);

                Assert.That(created, Is.True);
                Assert.That(status.IsResolved, Is.True);
                Assert.That(provider.EnvironmentId,
                    Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(message, Is.Null);
            }
        }

        [Test]
        public void LoginConfigurationConsumesResolvedCatalogRequestMetadata()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(
                    fixture.Catalog.TryGetEndpoint(
                        SimultriaEndpointIds.Login,
                        out ApiEndpointCatalogEntry login),
                    Is.True);
                login.Method = HttpMethod.PUT;
                login.Authentication =
                    ApiAuthenticationRequirement.Required;
                login.RequestPolicy.TimeoutSeconds = 17;

                SessionTokenEndpointConfig config =
                    SimultriaAuthenticationConfiguration.CreateLogin(
                        fixture.Composition,
                        SimultriaEnvironmentIds.Development);

                Assert.That(config.Method, Is.EqualTo(HttpMethod.PUT));
                Assert.That(config.TimeoutSeconds, Is.EqualTo(17));
                Assert.That(config.UseCurrentAccessTokenAsBearer, Is.True);
                Assert.That(config.InputDefinitions, Has.Count.EqualTo(2));
                Assert.That(
                    config.ResponseMapping.AccessTokenJsonPath,
                    Is.EqualTo("access_token"));
                Assert.That(
                    config.ResponseMapping.UseJwtExpiryFallback,
                    Is.True);
            }
        }

        [Test]
        public void ValidationConfigurationConsumesDisabledAuthentication()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(
                    fixture.Catalog.TryGetEndpoint(
                        SimultriaEndpointIds.ValidateAuthentication,
                        out ApiEndpointCatalogEntry validation),
                    Is.True);
                validation.Method = HttpMethod.PATCH;
                validation.Authentication =
                    ApiAuthenticationRequirement.Disabled;
                validation.RequestPolicy.TimeoutSeconds = 9;

                SessionTokenEndpointConfig config =
                    SimultriaAuthenticationConfiguration.CreateValidation(
                        fixture.Composition,
                        SimultriaEnvironmentIds.Development);

                Assert.That(config.Method, Is.EqualTo(HttpMethod.PATCH));
                Assert.That(config.TimeoutSeconds, Is.EqualTo(9));
                Assert.That(config.UseCurrentAccessTokenAsBearer, Is.False);
                Assert.That(config.InputDefinitions, Is.Empty);
            }
        }

        [Test]
        public void TokenConfigurationRejectsAmbiguousCatalogAuthentication()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                Assert.That(
                    fixture.Catalog.TryGetEndpoint(
                        SimultriaEndpointIds.Login,
                        out ApiEndpointCatalogEntry login),
                    Is.True);
                login.Authentication =
                    ApiAuthenticationRequirement.Optional;

                Assert.Throws<InvalidOperationException>(
                    () => SimultriaAuthenticationConfiguration.CreateLogin(
                        fixture.Composition,
                        SimultriaEnvironmentIds.Development));
            }
        }

        [Test]
        public void FactoryAcceptsCompatibleConnectionSettings()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentProfile testing = CreateBlankEnvironment(
                    SimultriaEnvironmentIds.Testing,
                    "Testing");
                ApiEnvironmentProfile acceptance = CreateBlankEnvironment(
                    SimultriaEnvironmentIds.Acceptance,
                    "Acceptance");
                ApiEnvironmentProfile production = CreateBlankEnvironment(
                    SimultriaEnvironmentIds.Production,
                    "Production");
                ApiConnectionSettings settings =
                    ApiConnectionSettings.CreateTransient(
                        new[]
                        {
                            fixture.Environment,
                            testing,
                            acceptance,
                            production
                        },
                        fixture.Definition);
                try
                {
                    bool created =
                        SimultriaAuthenticationProviderFactory.TryCreate(
                            settings,
                            SimultriaEnvironmentIds.Development,
                            new ApiClientSpy(),
                            out SimultriaAuthenticationProvider provider,
                            out Deucarian.API.Core.ApiEnvironmentStatus status,
                            out string message);

                    Assert.That(created, Is.True, message);
                    Assert.That(provider, Is.Not.Null);
                    Assert.That(status.IsResolved, Is.True);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(settings);
                    UnityEngine.Object.DestroyImmediate(testing);
                    UnityEngine.Object.DestroyImmediate(acceptance);
                    UnityEngine.Object.DestroyImmediate(production);
                }
            }
        }

        private static ApiEnvironmentProfile CreateBlankEnvironment(
            ApiEnvironmentId environmentId,
            string displayName)
        {
            ApiEnvironmentProfile environment =
                ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
            environment.EnvironmentId = environmentId.Value;
            environment.DisplayName = displayName;
            environment.Clients.Add(
                new ApiNamedClientDefinition
                {
                    ClientId = SimultriaClientIds.Primary.Value,
                    BaseUrl = string.Empty
                });
            return environment;
        }
    }
}
