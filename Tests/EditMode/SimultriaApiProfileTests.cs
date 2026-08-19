using Deucarian.API.Core;
using Deucarian.Simultria.API.Configuration;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiProfileTests
    {
        [Test]
        public void DevelopmentIdResolvesDocumentedEnvironmentWithoutSelectionState()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentStatus status = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Development);

                Assert.That(status.IsResolved, Is.True);
                Assert.That(status.EnvironmentId,
                    Is.EqualTo(SimultriaEnvironmentIds.Development));
                Assert.That(status.DisplayName,
                    Is.EqualTo("Simultria Development"));
                Assert.That(status.Message, Is.Null);
            }
        }

        [Test]
        public void ProfileReturnsSanitizedStatusWithoutApiOrigin()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                bool resolved = fixture.Profile.TryGetEnvironmentStatus(
                    SimultriaEnvironmentIds.Development,
                    out ApiEnvironmentStatus status,
                    out string message);

                Assert.That(resolved, Is.True);
                Assert.That(status.GetType().GetProperty("BaseUrl"), Is.Null);
                Assert.That(message, Is.Null);
            }
        }

        [Test]
        public void PackageDefaultProfileComposesDevelopmentCatalog()
        {
            SimultriaApiProfile profile = SimultriaApiProfileDefaults.Load();

            Assert.That(profile, Is.Not.Null);
            ApiComposition composition = profile.CreateComposition();
            ApiEnvironmentStatus status = composition.GetEnvironmentStatus(
                SimultriaEnvironmentIds.Development);
            Assert.That(status.IsResolved, Is.True);
            Assert.That(
                composition.ResolveEndpoint(
                        SimultriaEnvironmentIds.Development,
                        SimultriaEndpointIds.Login)
                    .Endpoint.Path,
                Does.EndWith("/api/v2/login"));
        }

        [Test]
        public void FutureEnvironmentIdsDoNotInventDeploymentUrls()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentStatus acceptance = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Acceptance);
                ApiEnvironmentStatus production = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Production);

                Assert.That(acceptance.IsResolved, Is.False);
                Assert.That(production.IsResolved, Is.False);
                Assert.That(acceptance.GetType().GetProperty("BaseUrl"), Is.Null);
                Assert.That(production.GetType().GetProperty("BaseUrl"), Is.Null);
            }
        }
    }
}
