using Deucarian.Simultria.API.Authentication;
using Deucarian.Simultria.API.Configuration;
using Deucarian.ViewerAuthentication;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaViewerAuthenticationProviderTests
    {
        [Test]
        public void ProviderOwnsOnlySimultriaEndpointAndInputSemantics()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                var provider = new SimultriaViewerAuthenticationProvider(
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
                    Is.InstanceOf<IInteractiveViewerAuthenticationAcquisitionProvider>());
                Assert.That(provider,
                    Is.InstanceOf<IViewerAuthenticationValidationProvider>());
            }
        }

        [Test]
        public void FactoryResolvesIdWithoutAConsumerOwnedUrl()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                bool created = SimultriaViewerAuthenticationProviderFactory
                    .TryCreate(
                        fixture.Profile,
                        SimultriaEnvironmentIds.Development,
                        new ApiClientSpy(),
                        out SimultriaViewerAuthenticationProvider provider,
                        out Deucarian.API.Core.ApiEnvironmentStatus status,
                        out string message);

                Assert.That(created, Is.True);
                Assert.That(status.IsResolved, Is.True);
                Assert.That(provider.EnvironmentId,
                    Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(message, Is.Null);
            }
        }
    }
}
