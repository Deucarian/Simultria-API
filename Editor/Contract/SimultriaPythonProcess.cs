using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaPythonProcess
    {
        internal static bool TryRun(
            IReadOnlyList<string> arguments,
            string workingDirectory,
            out string processOutput,
            out string error)
        {
            if (!TryFindPython(
                    workingDirectory,
                    out string executable,
                    out string prefix))
            {
                processOutput = string.Empty;
                error =
                    "Python 3 was not found. Install Python or set the " +
                    "DEUCARIAN_PYTHON environment variable.";
                return false;
            }

            var allArguments = new List<string>();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                allArguments.Add(prefix);
            }

            allArguments.AddRange(arguments);
            return TryRunProcess(
                executable,
                allArguments,
                workingDirectory,
                120000,
                out processOutput,
                out error);
        }

        internal static string QuoteArgument(string value)
        {
            value = value ?? string.Empty;
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            int pendingBackslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    pendingBackslashes++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', pendingBackslashes * 2 + 1);
                    builder.Append('"');
                    pendingBackslashes = 0;
                    continue;
                }

                builder.Append('\\', pendingBackslashes);
                pendingBackslashes = 0;
                builder.Append(character);
            }

            builder.Append('\\', pendingBackslashes * 2);
            builder.Append('"');
            return builder.ToString();
        }

        private static bool TryFindPython(
            string workingDirectory,
            out string executable,
            out string prefix)
        {
            string configured = Environment.GetEnvironmentVariable(
                "DEUCARIAN_PYTHON");
            var candidates = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(configured))
            {
                candidates.Add(new KeyValuePair<string, string>(
                    configured.Trim(),
                    string.Empty));
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                candidates.Add(new KeyValuePair<string, string>("py", "-3"));
            }

            candidates.Add(new KeyValuePair<string, string>(
                "python3",
                string.Empty));
            candidates.Add(new KeyValuePair<string, string>(
                "python",
                string.Empty));
            foreach (KeyValuePair<string, string> candidate in candidates)
            {
                var versionArguments = new List<string>();
                if (!string.IsNullOrWhiteSpace(candidate.Value))
                {
                    versionArguments.Add(candidate.Value);
                }

                versionArguments.Add("--version");
                if (TryRunProcess(
                        candidate.Key,
                        versionArguments,
                        workingDirectory,
                        5000,
                        out _,
                        out _))
                {
                    executable = candidate.Key;
                    prefix = candidate.Value;
                    return true;
                }
            }

            executable = null;
            prefix = null;
            return false;
        }

        private static bool TryRunProcess(
            string executable,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            int timeoutMilliseconds,
            out string processOutput,
            out string error)
        {
            processOutput = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = JoinArguments(arguments),
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Task<string> standardOutput =
                        process.StandardOutput.ReadToEndAsync();
                    Task<string> standardError =
                        process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        process.Kill();
                        error = "The contract generator timed out.";
                        return false;
                    }

                    Task.WaitAll(standardOutput, standardError);
                    processOutput = (standardOutput.Result + "\n" +
                        standardError.Result).Trim();
                    if (process.ExitCode != 0)
                    {
                        error = string.IsNullOrWhiteSpace(processOutput)
                            ? "The contract generator failed."
                            : processOutput;
                        return false;
                    }
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error =
                    "The contract generator could not start (" +
                    exception.GetType().Name + ").";
                return false;
            }
        }

        private static string JoinArguments(IReadOnlyList<string> values)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < values.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(QuoteArgument(values[index]));
            }

            return builder.ToString();
        }
    }
}
