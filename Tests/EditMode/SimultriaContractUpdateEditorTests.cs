using System.IO;
using Deucarian.Simultria.API.Editor;
using NUnit.Framework;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    public sealed class SimultriaContractUpdateEditorTests
    {
        [TestCase("53f2ee7")]
        [TestCase("53f2ee778c5ec3d22763c86537850061642317cb")]
        public void BackendRevisionRequiresHexadecimalGitCommit(string value)
        {
            Assert.That(
                SimultriaContractUpdateService.IsValidSourceRevision(value),
                Is.True);
        }

        [TestCase("")]
        [TestCase("development")]
        [TestCase("not-a-git-commit")]
        public void BackendRevisionRejectsMissingOrSymbolicValues(string value)
        {
            Assert.That(
                SimultriaContractUpdateService.IsValidSourceRevision(value),
                Is.False);
        }

        [Test]
        public void IncomingContractChangeComparesSourceHashes()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "first contract");
                string currentHash =
                    SimultriaContractUpdateService.ComputeSha256(path);
                var manifest = new SimultriaContractManifestDocument
                {
                    source = new SimultriaContractSource
                    {
                        sha256 = currentHash
                    }
                };

                Assert.That(
                    SimultriaContractUpdateService.HasIncomingContractChange(
                        path,
                        manifest),
                    Is.False);

                File.WriteAllText(path, "second contract");

                Assert.That(
                    SimultriaContractUpdateService.HasIncomingContractChange(
                        path,
                        manifest),
                    Is.True);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void PythonArgumentsPreserveWindowsPathsWithSpaces()
        {
            Assert.That(
                SimultriaPythonProcess.QuoteArgument(
                    @"C:\Backend Work\storage\openapi.yaml"),
                Is.EqualTo(
                    "\"C:\\Backend Work\\storage\\openapi.yaml\""));
        }
    }
}
