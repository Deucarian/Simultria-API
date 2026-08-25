using System;
using System.Collections.Generic;
using Deucarian.Simultria.API.Models;
using Deucarian.Simultria.API.Services;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaViewerModelResolverTests
    {
        [Test]
        public void RequestedVersionWinsOverActiveAndLatestFallbacks()
        {
            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    17,
                    ProjectWithActiveVersion(
                        99,
                        Version(17, "1", order: "1"),
                        Version(99, "9", order: "9")));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ModelVersionId, Is.EqualTo(17));
            Assert.That(result.UsedRequestedVersion, Is.True);
            Assert.That(result.UsedActiveVersion, Is.False);
        }

        [Test]
        public void UnpinnedSelectionUsesActiveVersionInsteadOfLatest()
        {
            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    null,
                    ProjectWithActiveVersion(
                        17,
                        Version(17, "1", order: "1"),
                        Version(99, "9", order: "9")));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ModelVersionId, Is.EqualTo(17));
            Assert.That(result.UsedRequestedVersion, Is.False);
            Assert.That(result.UsedActiveVersion, Is.True);
            Assert.That(
                result.Message,
                Is.EqualTo("Resolved the active Simultria model version."));
        }

        [Test]
        public void UnpinnedSelectionUsesLatestOnlyWhenActiveVersionIsMissing()
        {
            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    null,
                    Projects(
                        Version(17, "1", order: "1"),
                        Version(99, "9", order: "9")));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ModelVersionId, Is.EqualTo(99));
            Assert.That(result.UsedRequestedVersion, Is.False);
            Assert.That(result.UsedActiveVersion, Is.False);
            Assert.That(
                result.Message,
                Is.EqualTo("Resolved the latest Simultria model version."));
        }

        [Test]
        public void MissingActiveVersionDetailsFailWithoutUsingLatestVersion()
        {
            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    null,
                    ProjectWithActiveVersion(
                        17,
                        Version(99, "9", order: "9")));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.ErrorCode,
                Is.EqualTo(
                    SimultriaViewerModelErrorCodes.ModelVersionNotFound));
            Assert.That(result.Message, Does.StartWith("The active Simultria"));
            Assert.That(result.DownloadUrl, Is.Null);
        }

        [Test]
        public void DetailedActiveVersionWorksWhenVersionCollectionsAreNull()
        {
            var project = new SimultriaProjectDto
            {
                Id = 1,
                Models = new List<SimultriaModelDto>
                {
                    new SimultriaModelDto
                    {
                        Id = 2,
                        ActiveVersion = Version(17, "1"),
                        ModelVersions = null,
                        Versions = null
                    }
                }
            };

            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    null,
                    new[] { project });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ModelVersionId, Is.EqualTo(17));
            Assert.That(result.UsedActiveVersion, Is.True);
        }

        [Test]
        public void LatestSelectionUsesOrderedDeterministicFallbackChain()
        {
            SimultriaModelVersionDto byOrder = Version(10, "1.0.0", "5");
            SimultriaModelVersionDto higherSemantic =
                Version(20, "99.0.0", "4");

            SimultriaModelVersionDto selected =
                SimultriaViewerModelResolver.SelectLatestVersion(
                    new[] { higherSemantic, byOrder });

            Assert.That(selected, Is.SameAs(byOrder));
        }

        [Test]
        public void EqualOrderFallsThroughVersionNumberSemanticDatesAndId()
        {
            var older = Version(10, "2.0.0", "5", "7");
            older.UpdatedAtUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
            var newer = Version(20, "2.0.0", "5", "7");
            newer.UpdatedAtUtc = DateTimeOffset.Parse("2026-02-01T00:00:00Z");

            SimultriaModelVersionDto selected =
                SimultriaViewerModelResolver.SelectLatestVersion(
                    new[] { older, newer });

            Assert.That(selected, Is.SameAs(newer));
        }

        [Test]
        public void NestedRequestedProjectCanResolveItsModel()
        {
            var root = new SimultriaProjectDto
            {
                Id = 1,
                Name = "Root",
                SubProjects = new List<SimultriaProjectDto>
                {
                    new SimultriaProjectDto
                    {
                        Id = 5,
                        Name = "Nested",
                        Models = new List<SimultriaModelDto>
                        {
                            new SimultriaModelDto
                            {
                                Id = 8,
                                Name = "Model",
                                Versions = new List<SimultriaModelVersionDto>
                                {
                                    Version(13, "1.0.0")
                                }
                            }
                        }
                    }
                }
            };

            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    5,
                    8,
                    null,
                    new[] { root });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ProjectId, Is.EqualTo(5));
            Assert.That(result.ModelId, Is.EqualTo(8));
            Assert.That(result.ModelVersionId, Is.EqualTo(13));
            Assert.That(result.UsedActiveVersion, Is.False);
        }

        [Test]
        public void MissingRequestedVersionReturnsStableErrorCode()
        {
            SimultriaViewerModelResolveResult result =
                SimultriaViewerModelResolver.ResolveFromProjects(
                    1,
                    2,
                    404,
                    Projects(Version(17, "1.0.0")));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo(SimultriaViewerModelErrorCodes.ModelVersionNotFound));
            Assert.That(result.DownloadUrl, Is.Null);
        }

        private static IEnumerable<SimultriaProjectDto> Projects(
            params SimultriaModelVersionDto[] versions)
        {
            return new[]
            {
                new SimultriaProjectDto
                {
                    Id = 1,
                    Name = "Project",
                    Models = new List<SimultriaModelDto>
                    {
                        new SimultriaModelDto
                        {
                            Id = 2,
                            Name = "Model",
                            Versions = new List<SimultriaModelVersionDto>(
                                versions)
                        }
                    }
                }
            };
        }

        private static IEnumerable<SimultriaProjectDto> ProjectWithActiveVersion(
            int activeVersionId,
            params SimultriaModelVersionDto[] versions)
        {
            IEnumerable<SimultriaProjectDto> projects = Projects(versions);
            foreach (SimultriaProjectDto project in projects)
            {
                project.Models[0].ActiveVersion =
                    new SimultriaModelVersionDto { Id = activeVersionId };
            }

            return projects;
        }

        private static SimultriaModelVersionDto Version(
            int id,
            string version,
            string order = null,
            string versionNumber = null)
        {
            return new SimultriaModelVersionDto
            {
                Id = id,
                Name = "Version " + id,
                Version = version,
                Order = order,
                VersionNumber = versionNumber,
                DownloadUrl = "https://example.invalid/model-" + id
            };
        }
    }
}
