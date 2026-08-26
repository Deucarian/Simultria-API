using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaContractUpdateService
    {
        internal const string ManifestRelativePath =
            "Documentation~/Generated/SimultriaApiV2Contract.manifest.json";
        internal const string DefaultSpecRelativePath =
            "ContractSource~/openapi.yaml";

        private const string UpdateScriptRelativePath =
            "Tools~/update_contract.py";
        private const string ChangeReportFileName = "change-report.json";
        private const string ChangeReportMarkdownFileName =
            "change-report.md";

        private static readonly Regex GitRevisionPattern = new Regex(
            "^[0-9a-fA-F]{7,64}$",
            RegexOptions.CultureInvariant);

        internal static string PackageRoot
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(
                    typeof(SimultriaContractUpdateService).Assembly);
                return package != null ? package.resolvedPath : null;
            }
        }

        internal static string DefaultIncomingSpecPath
        {
            get
            {
                string root = PackageRoot;
                return string.IsNullOrWhiteSpace(root)
                    ? string.Empty
                    : Path.Combine(root, DefaultSpecRelativePath);
            }
        }

        internal static bool IsEditablePackage
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(
                    typeof(SimultriaContractUpdateService).Assembly);
                return package != null &&
                    (package.source == PackageSource.Local ||
                     package.source == PackageSource.Embedded);
            }
        }

        internal static SimultriaContractUpdateResult Preview(
            string specPath,
            string sourceRevision)
        {
            return Execute(specPath, sourceRevision, false);
        }

        internal static SimultriaContractUpdateResult Apply(
            string specPath,
            string sourceRevision)
        {
            if (!IsEditablePackage)
            {
                return Failure(
                    "The installed package is read-only. Reference the " +
                    "Simultria API checkout as a local or embedded package " +
                    "before applying a contract update.");
            }

            SimultriaContractUpdateResult result = Execute(
                specPath,
                sourceRevision,
                true);
            if (result.Succeeded)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            return result;
        }

        internal static bool TryLoadCurrentManifest(
            out SimultriaContractManifestDocument manifest,
            out string error)
        {
            string root = PackageRoot;
            if (string.IsNullOrWhiteSpace(root))
            {
                manifest = null;
                error = "The Simultria API package root could not be resolved.";
                return false;
            }

            return TryLoadJson(
                Path.Combine(root, ManifestRelativePath),
                out manifest,
                out error);
        }

        internal static bool HasIncomingContractChange(
            string specPath,
            SimultriaContractManifestDocument currentManifest)
        {
            if (string.IsNullOrWhiteSpace(specPath) ||
                !File.Exists(specPath) ||
                currentManifest == null ||
                currentManifest.source == null)
            {
                return false;
            }

            string incomingHash = ComputeSha256(specPath);
            return !string.Equals(
                incomingHash,
                currentManifest.source.sha256,
                StringComparison.OrdinalIgnoreCase);
        }

        internal static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = algorithm.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        internal static bool IsValidSourceRevision(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                GitRevisionPattern.IsMatch(value.Trim());
        }

        private static SimultriaContractUpdateResult Execute(
            string specPath,
            string sourceRevision,
            bool apply)
        {
            if (!TryValidateRequest(
                    specPath,
                    sourceRevision,
                    out string normalizedSpec,
                    out string normalizedRevision,
                    out string validationError))
            {
                return Failure(validationError);
            }

            string packageRoot = PackageRoot;
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                return Failure(
                    "The Simultria API package root could not be resolved.");
            }

            string previewRoot = GetPreviewRoot();
            if (!TryPreparePreviewDirectory(previewRoot, out string error))
            {
                return Failure(error);
            }

            string outputRoot = apply ? packageRoot : previewRoot;
            string reportPath = Path.Combine(
                previewRoot,
                ChangeReportFileName);
            string reportMarkdownPath = Path.Combine(
                previewRoot,
                ChangeReportMarkdownFileName);
            var arguments = new List<string>
            {
                Path.Combine(packageRoot, UpdateScriptRelativePath),
                "--spec",
                normalizedSpec,
                "--source-revision",
                normalizedRevision,
                "--output-root",
                outputRoot,
                "--change-report-out",
                reportPath,
                "--change-report-markdown-out",
                reportMarkdownPath
            };

            if (!SimultriaPythonProcess.TryRun(
                    arguments,
                    packageRoot,
                    out string processOutput,
                    out error))
            {
                return new SimultriaContractUpdateResult
                {
                    Succeeded = false,
                    Message = error,
                    ProcessOutput = processOutput,
                    PreviewRoot = previewRoot
                };
            }

            if (!TryLoadJson(
                    Path.Combine(outputRoot, ManifestRelativePath),
                    out SimultriaContractManifestDocument manifest,
                    out error) ||
                !TryLoadJson(
                    reportPath,
                    out SimultriaContractChangeReport report,
                    out error))
            {
                return Failure(error);
            }

            return new SimultriaContractUpdateResult
            {
                Succeeded = true,
                Message = apply
                    ? "The package contract was regenerated successfully."
                    : "Preview completed without changing package files.",
                ProcessOutput = processOutput,
                PreviewRoot = previewRoot,
                Manifest = manifest,
                ChangeReport = report
            };
        }

        private static bool TryValidateRequest(
            string specPath,
            string sourceRevision,
            out string normalizedSpec,
            out string normalizedRevision,
            out string error)
        {
            normalizedSpec = specPath != null
                ? specPath.Trim()
                : string.Empty;
            normalizedRevision = sourceRevision != null
                ? sourceRevision.Trim().ToLowerInvariant()
                : string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSpec) ||
                normalizedSpec.IndexOf(
                    "://",
                    StringComparison.Ordinal) >= 0 ||
                !File.Exists(normalizedSpec))
            {
                error = "Choose a local OpenAPI JSON or YAML file.";
                return false;
            }

            string extension = Path.GetExtension(normalizedSpec);
            if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase))
            {
                error = "The OpenAPI file must use .json, .yaml, or .yml.";
                return false;
            }

            if (!IsValidSourceRevision(normalizedRevision))
            {
                error =
                    "Enter the 7-64 character hexadecimal backend Git commit " +
                    "that generated this specification.";
                return false;
            }

            normalizedSpec = Path.GetFullPath(normalizedSpec);
            error = null;
            return true;
        }

        private static string GetPreviewRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                .FullName;
            return Path.Combine(
                projectRoot,
                "Library",
                "Deucarian",
                "SimultriaApiContract",
                "Preview");
        }

        private static bool TryPreparePreviewDirectory(
            string previewRoot,
            out string error)
        {
            try
            {
                string fullPath = Path.GetFullPath(previewRoot);
                string projectLibrary = Path.GetFullPath(
                    Path.Combine(
                        Directory.GetParent(Application.dataPath).FullName,
                        "Library"));
                if (!fullPath.StartsWith(
                        projectLibrary + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                {
                    error = "The contract preview path escaped Project/Library.";
                    return false;
                }

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }

                Directory.CreateDirectory(fullPath);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error =
                    "The contract preview directory could not be prepared (" +
                    exception.GetType().Name + ").";
                return false;
            }
        }

        private static bool TryLoadJson<T>(
            string path,
            out T value,
            out string error)
            where T : class
        {
            value = null;
            try
            {
                if (!File.Exists(path))
                {
                    error = "Generated contract file is missing: " + path;
                    return false;
                }

                value = JsonUtility.FromJson<T>(File.ReadAllText(path));
                if (value == null)
                {
                    error = "Generated contract JSON could not be parsed.";
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error =
                    "Generated contract JSON could not be read (" +
                    exception.GetType().Name + ").";
                return false;
            }
        }

        private static SimultriaContractUpdateResult Failure(string message)
        {
            return new SimultriaContractUpdateResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
