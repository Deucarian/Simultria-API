using System;
using System.IO;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    internal sealed class SimultriaContractUpdateWindow : EditorWindow
    {
        internal const string MenuPath =
            "Tools/Deucarian/Simultria API/Open Contract Updater";

        private const string SpecPathPreference =
            "Deucarian.Simultria.API.Contract.SpecPath";
        private const string RevisionPreference =
            "Deucarian.Simultria.API.Contract.BackendRevision";

        private string specPath;
        private string sourceRevision;
        private Vector2 scroll;
        private SimultriaContractManifestDocument currentManifest;
        private SimultriaContractUpdateResult lastResult;
        private string previewSpecHash;
        private string previewRevision;
        private bool showChangeDetails;

        [MenuItem(MenuPath, false, 210)]
        internal static SimultriaContractUpdateWindow OpenWindow()
        {
            var window = GetWindow<SimultriaContractUpdateWindow>();
            window.titleContent = new GUIContent("Simultria API Contract");
            window.minSize = new Vector2(620f, 540f);
            window.Show();
            return window;
        }

        internal void UseIncomingSpec(string path)
        {
            specPath = path;
            ClearPreview();
            SavePreferences();
            Repaint();
        }

        private void OnEnable()
        {
            specPath = EditorPrefs.GetString(
                SpecPathPreference,
                SimultriaContractUpdateService.DefaultIncomingSpecPath);
            sourceRevision = EditorPrefs.GetString(
                RevisionPreference,
                string.Empty);
            ReloadCurrentManifest();
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        private void OnGUI()
        {
            DeucarianEditorWindowChrome.DrawImGuiWindowBackground(
                new Rect(0f, 0f, position.width, position.height));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12f);
                using (new EditorGUILayout.VerticalScope())
                {
                    DeucarianEditorCards.DrawHeaderCard(
                        "Simultria API contract updater",
                        "Preview and regenerate the package catalog, coverage, " +
                        "provenance and local endpoint documentation from one " +
                        "backend-generated OpenAPI file.",
                        SimultriaContractUpdateService.IsEditablePackage
                            ? "Editable package"
                            : "Read-only package");
                    DrawCurrentContract();
                    DrawSource();
                    DrawActions();
                    DrawResult();
                    DeucarianEditorStatusPanel.DrawStatusBar(
                        "Local files only",
                        "No backend credentials",
                        "Human review before merge");
                }

                GUILayout.Space(12f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCurrentContract()
        {
            DeucarianEditorCards.DrawCard(
                "Installed snapshot",
                () =>
                {
                    if (currentManifest == null ||
                        currentManifest.source == null ||
                        currentManifest.catalog == null)
                    {
                        DeucarianEditorStatusPanel.DrawStatusCard(
                            "Generated contract provenance is missing or invalid.",
                            DeucarianEditorStatus.Error);
                        return;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DeucarianEditorStatusBadge.Draw(
                            currentManifest.coverage != null &&
                            currentManifest.coverage.snapshotCoverageComplete
                                ? "Snapshot complete"
                                : "Coverage incomplete",
                            currentManifest.coverage != null &&
                            currentManifest.coverage.snapshotCoverageComplete
                                ? DeucarianEditorStatus.Success
                                : DeucarianEditorStatus.Error,
                            GUILayout.Width(140f));
                        GUILayout.Space(6f);
                        EditorGUILayout.LabelField(
                            currentManifest.catalog.operationCount +
                            " operations · " +
                            currentManifest.catalog.reviewedStableOperationCount +
                            " stable",
                            EditorStyles.miniLabel);
                    }

                    EditorGUILayout.LabelField(
                        "Backend commit",
                        ShortValue(currentManifest.source.backendRevision, 16));
                    EditorGUILayout.LabelField(
                        "Contract SHA-256",
                        ShortValue(currentManifest.source.sha256, 20));
                    EditorGUILayout.LabelField(
                        "OpenAPI",
                        currentManifest.source.openapiVersion ?? "Unknown");
                    string incomingPath =
                        SimultriaContractUpdateService.DefaultIncomingSpecPath;
                    if (SimultriaContractUpdateService.HasIncomingContractChange(
                            incomingPath,
                            currentManifest))
                    {
                        DeucarianEditorStatusPanel.DrawStatusCard(
                            "A different OpenAPI file is waiting in " +
                            "ContractSource~. Preview it before updating.",
                            DeucarianEditorStatus.Warning);
                    }
                },
                "The runtime uses this pinned local catalog and never downloads " +
                "routes or credentials.");
        }

        private void DrawSource()
        {
            DeucarianEditorCards.DrawCard(
                "Incoming backend contract",
                () =>
                {
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        specPath = EditorGUILayout.TextField(
                            "OpenAPI file",
                            specPath ?? string.Empty);
                        if (DeucarianEditorButtons.Secondary(
                                "Browse",
                                true,
                                GUILayout.Width(82f)))
                        {
                            string selected = EditorUtility.OpenFilePanel(
                                "Select backend OpenAPI contract",
                                InitialBrowseDirectory(),
                                string.Empty);
                            if (!string.IsNullOrWhiteSpace(selected))
                            {
                                specPath = selected;
                            }
                        }
                    }

                    sourceRevision = EditorGUILayout.TextField(
                        "Backend commit",
                        sourceRevision ?? string.Empty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ClearPreview();
                        SavePreferences();
                    }

                    if (!string.IsNullOrWhiteSpace(sourceRevision) &&
                        !SimultriaContractUpdateService.IsValidSourceRevision(
                            sourceRevision))
                    {
                        EditorGUILayout.HelpBox(
                            "Use the hexadecimal Git commit that generated the " +
                            "specification, not a branch name.",
                            MessageType.Warning);
                    }
                    EditorGUILayout.HelpBox(
                        "Scribe output is read locally. The original OpenAPI " +
                        "file is not copied into runtime assets and no backend " +
                        "login is performed.",
                        MessageType.Info);
                },
                "Expected backend output: storage/app/scribe/openapi.yaml");
        }

        private void DrawActions()
        {
            DeucarianEditorCards.DrawCard(
                "Update workflow",
                () =>
                {
                    bool requestValid = File.Exists(specPath ?? string.Empty) &&
                        SimultriaContractUpdateService.IsValidSourceRevision(
                            sourceRevision);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (DeucarianEditorButtons.Secondary(
                                "Preview changes",
                                requestValid,
                                GUILayout.MinWidth(150f)))
                        {
                            RunPreview();
                        }

                        bool previewCurrent = IsPreviewCurrent();
                        if (DeucarianEditorButtons.Primary(
                                "Apply generated update",
                                previewCurrent &&
                                SimultriaContractUpdateService.IsEditablePackage,
                                GUILayout.MinWidth(190f)))
                        {
                            RunApply();
                        }
                    }

                    if (!SimultriaContractUpdateService.IsEditablePackage)
                    {
                        EditorGUILayout.HelpBox(
                            "Preview is available, but package files can only be " +
                            "updated from a local or embedded package checkout.",
                            MessageType.Info);
                    }
                    else if (lastResult == null || !IsPreviewCurrent())
                    {
                        EditorGUILayout.HelpBox(
                            "Preview the exact file and backend commit before " +
                            "applying generated changes.",
                            MessageType.Info);
                    }
                });
        }

        private void DrawResult()
        {
            if (lastResult == null)
            {
                return;
            }

            DeucarianEditorCards.DrawCard(
                "Latest result",
                () =>
                {
                    DeucarianEditorStatusPanel.DrawStatusCard(
                        lastResult.Message,
                        lastResult.Succeeded
                            ? SimultriaContractChangeView.GetStatus(
                                lastResult.ChangeReport)
                            : DeucarianEditorStatus.Error);
                    if (!lastResult.Succeeded)
                    {
                        if (!string.IsNullOrWhiteSpace(lastResult.ProcessOutput))
                        {
                            EditorGUILayout.TextArea(
                                lastResult.ProcessOutput,
                                GUILayout.MinHeight(70f));
                        }

                        return;
                    }

                    SimultriaContractChangeReport report =
                        lastResult.ChangeReport;
                    if (report == null || report.summary == null)
                    {
                        return;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DeucarianEditorStatusBadge.Draw(
                            "+" + report.summary.added,
                            report.summary.added > 0
                                ? DeucarianEditorStatus.Info
                                : DeucarianEditorStatus.Disabled,
                            GUILayout.Width(64f));
                        DeucarianEditorStatusBadge.Draw(
                            "−" + report.summary.removed,
                            report.summary.removed > 0
                                ? DeucarianEditorStatus.Error
                                : DeucarianEditorStatus.Disabled,
                            GUILayout.Width(64f));
                        DeucarianEditorStatusBadge.Draw(
                            "Δ" + report.summary.changed,
                            report.summary.changed > 0
                                ? DeucarianEditorStatus.Warning
                                : DeucarianEditorStatus.Disabled,
                            GUILayout.Width(64f));
                    }

                    if (report.breakingOrSecurityReviewRequired)
                    {
                        EditorGUILayout.HelpBox(
                            "A route, method, authentication, logging rule, or " +
                            "existing endpoint was changed or removed. Review " +
                            "the package diff before merging.",
                            MessageType.Warning);
                    }
                    showChangeDetails = EditorGUILayout.Foldout(
                        showChangeDetails,
                        "Endpoint change details",
                        true);
                    if (showChangeDetails)
                    {
                        SimultriaContractChangeView.DrawDetails(report);
                    }
                });
        }

        private void RunPreview()
        {
            lastResult = SimultriaContractUpdateService.Preview(
                specPath,
                sourceRevision);
            if (lastResult.Succeeded)
            {
                previewSpecHash =
                    SimultriaContractUpdateService.ComputeSha256(specPath);
                previewRevision = sourceRevision.Trim().ToLowerInvariant();
            }
        }

        private void RunApply()
        {
            SimultriaContractChangeReport report = lastResult.ChangeReport;
            string warning = report != null &&
                report.breakingOrSecurityReviewRequired
                    ? "Breaking or security-sensitive endpoint changes were " +
                      "detected. Apply the generated files for review?"
                    : "Apply all generated contract files to the package checkout?";
            if (!EditorUtility.DisplayDialog(
                    "Apply Simultria API contract update",
                    warning,
                    "Apply for review",
                    "Cancel"))
            {
                return;
            }

            lastResult = SimultriaContractUpdateService.Apply(
                specPath,
                sourceRevision);
            if (lastResult.Succeeded)
            {
                ReloadCurrentManifest();
                previewSpecHash =
                    SimultriaContractUpdateService.ComputeSha256(specPath);
                previewRevision = sourceRevision.Trim().ToLowerInvariant();
            }
        }

        private bool IsPreviewCurrent()
        {
            return lastResult != null &&
                lastResult.Succeeded &&
                File.Exists(specPath ?? string.Empty) &&
                string.Equals(
                    previewSpecHash,
                    SimultriaContractUpdateService.ComputeSha256(specPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    previewRevision,
                    sourceRevision != null
                        ? sourceRevision.Trim().ToLowerInvariant()
                        : string.Empty,
                    StringComparison.Ordinal);
        }

        private void ReloadCurrentManifest()
        {
            SimultriaContractUpdateService.TryLoadCurrentManifest(
                out currentManifest,
                out _);
        }

        private void ClearPreview()
        {
            lastResult = null;
            previewSpecHash = null;
            previewRevision = null;
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(SpecPathPreference, specPath ?? string.Empty);
            EditorPrefs.SetString(
                RevisionPreference,
                sourceRevision ?? string.Empty);
        }

        private string InitialBrowseDirectory()
        {
            if (!string.IsNullOrWhiteSpace(specPath))
            {
                string fullPath = Path.GetFullPath(specPath);
                if (File.Exists(fullPath))
                {
                    return Path.GetDirectoryName(fullPath);
                }
            }

            return SimultriaContractUpdateService.PackageRoot ?? string.Empty;
        }

        private static string ShortValue(string value, int length)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= length)
            {
                return value ?? "Unknown";
            }

            return value.Substring(0, length) + "…";
        }
    }
}
