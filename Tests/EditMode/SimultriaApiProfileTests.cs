using System;
using Deucarian.API.Configuration;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Configuration;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaApiProfileTests
    {
        [Test]
        public void StandardDescriptorsAreStableAndCanonicallyOrdered()
        {
            var descriptors = SimultriaEnvironmentDescriptors.Standard;

            Assert.That(descriptors.Count, Is.EqualTo(4));
            AssertDescriptor(
                descriptors[0],
                SimultriaEnvironmentIds.Development,
                ApiEnvironmentStage.Development,
                "Development");
            AssertDescriptor(
                descriptors[1],
                SimultriaEnvironmentIds.Testing,
                ApiEnvironmentStage.Testing,
                "Testing");
            AssertDescriptor(
                descriptors[2],
                SimultriaEnvironmentIds.Acceptance,
                ApiEnvironmentStage.Acceptance,
                "Acceptance");
            AssertDescriptor(
                descriptors[3],
                SimultriaEnvironmentIds.Production,
                ApiEnvironmentStage.Production,
                "Production");
        }

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
        public void PackageDefaultProfileContainsNoDeploymentUrl()
        {
            SimultriaApiProfile profile = SimultriaApiProfileDefaults.Load();

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Environments.Count, Is.EqualTo(1));
            Assert.That(profile.Environments[0].Clients.Count, Is.EqualTo(1));
            Assert.That(profile.Environments[0].Clients[0].BaseUrl, Is.Empty);
            ApiComposition composition = profile.CreateComposition();
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                ApiEnvironmentStatus status = composition.GetEnvironmentStatus(
                    descriptor.EnvironmentId);
                Assert.That(
                    status.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(status.Stage, Is.EqualTo(descriptor.Stage));
                Assert.That(
                    composition.TryResolveEndpoint(
                        descriptor.EnvironmentId,
                        SimultriaEndpointIds.Login,
                        out _,
                        out string message),
                    Is.False);
                Assert.That(message, Does.Contain("not configured"));
            }
        }

        [Test]
        public void FutureEnvironmentIdsDoNotInventDeploymentUrls()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentStatus testing = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Testing);
                ApiEnvironmentStatus acceptance = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Acceptance);
                ApiEnvironmentStatus production = fixture.Composition
                    .GetEnvironmentStatus(SimultriaEnvironmentIds.Production);

                Assert.That(
                    testing.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(
                    acceptance.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(
                    production.Availability,
                    Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                Assert.That(testing.Message, Does.Contain("not configured"));
                Assert.That(acceptance.GetType().GetProperty("BaseUrl"), Is.Null);
                Assert.That(production.GetType().GetProperty("BaseUrl"), Is.Null);
            }
        }

        [Test]
        public void BlankKnownProfileIsUnconfiguredWithoutBlockingDevelopment()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentProfile testing = CreateEnvironment(
                    SimultriaEnvironmentIds.Testing,
                    "Testing",
                    string.Empty);
                SimultriaApiProfile profile = SimultriaApiProfile.CreateTransient(
                    new[] { fixture.Environment, testing },
                    fixture.Catalog);
                try
                {
                    ApiComposition composition = profile.CreateComposition();

                    Assert.That(
                        composition.GetEnvironmentStatus(
                            SimultriaEnvironmentIds.Development).Availability,
                        Is.EqualTo(ApiEnvironmentAvailability.Configured));
                    ApiEnvironmentStatus status = composition
                        .GetEnvironmentStatus(SimultriaEnvironmentIds.Testing);
                    Assert.That(
                        status.Availability,
                        Is.EqualTo(ApiEnvironmentAvailability.Unconfigured));
                    Assert.That(status.Message, Does.Contain("not configured"));
                    Assert.That(
                        composition.TryResolveClient(
                            SimultriaEnvironmentIds.Testing,
                            SimultriaClientIds.Primary,
                            out _,
                            out string message),
                        Is.False);
                    Assert.That(message, Does.Contain("not configured"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                    UnityEngine.Object.DestroyImmediate(testing);
                }
            }
        }

        [Test]
        public void NonEmptyInvalidHostFailsClosed()
        {
            using (var fixture = new SimultriaTestComposition())
            {
                ApiEnvironmentProfile testing = CreateEnvironment(
                    SimultriaEnvironmentIds.Testing,
                    "Testing",
                    "not-an-absolute-http-url");
                SimultriaApiProfile profile = SimultriaApiProfile.CreateTransient(
                    new[] { fixture.Environment, testing },
                    fixture.Catalog);
                try
                {
                    Assert.Throws<ArgumentException>(
                        () => profile.CreateComposition());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                    UnityEngine.Object.DestroyImmediate(testing);
                }
            }
        }

        private static ApiEnvironmentProfile CreateEnvironment(
            ApiEnvironmentId environmentId,
            string displayName,
            string baseUrl)
        {
            ApiEnvironmentProfile environment =
                ScriptableObject.CreateInstance<ApiEnvironmentProfile>();
            environment.EnvironmentId = environmentId.Value;
            environment.DisplayName = displayName;
            environment.Clients.Add(
                new ApiNamedClientDefinition
                {
                    ClientId = SimultriaClientIds.Primary.Value,
                    BaseUrl = baseUrl
                });
            return environment;
        }

        private static void AssertDescriptor(
            ApiEnvironmentDescriptor descriptor,
            ApiEnvironmentId environmentId,
            ApiEnvironmentStage stage,
            string displayName)
        {
            Assert.That(descriptor.EnvironmentId, Is.EqualTo(environmentId));
            Assert.That(descriptor.Stage, Is.EqualTo(stage));
            Assert.That(descriptor.DisplayName, Is.EqualTo(displayName));
        }
    }
}
