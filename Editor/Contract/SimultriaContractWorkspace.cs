using System;
using System.Linq;
using Deucarian.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Simultria.API.Editor
{
    internal sealed class SimultriaContractWorkspace : IDisposable
    {
        private readonly SimultriaContractUpdateWindow owner;
        private readonly DeucarianEditorWorkspace workspace;
        private readonly DeucarianEditorWorkspaceForm versions, generation;
        private readonly VisualElement changes;
        private readonly Button check, apply;
        internal SimultriaContractWorkspace(VisualElement root, SimultriaContractUpdateWindow owner)
        {
            this.owner = owner;
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "API contract";
            workspace.Subtitle.text = "Review changes before updating generated code.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, DeucarianToolIds.SimultriaContractUpdater);
            var scroll = Controls.Scroll("contract-workspace"); workspace.Content.Add(scroll);
            var service = Controls.Panel("contract-service"); scroll.Add(service);
            new DeucarianEditorWorkspaceForm(service).ReadOnly("contract-service-name", "Service", () => "Simultria API");
            var comparison = Controls.Panel("contract-versions"); comparison.AddToClassList("dw-summary-row"); scroll.Add(comparison);
            var values = Controls.Region(null, "dw-action-summary-copy"); comparison.Add(values);
            versions = new DeucarianEditorWorkspaceForm(values);
            values.AddToClassList("dw-property-summary");
            versions.ReadOnly("contract-current", "Current version", () => Short(owner.Current?.source?.backendRevision, "Local snapshot"));
            versions.ReadOnly("contract-available", "Available version", () => Short(owner.LastResult?.Manifest?.source?.backendRevision, "Not checked"));
            check = Controls.Button("Check for updates", owner.RunPreview, true); check.name = "contract-check"; comparison.Add(check);
            changes = Controls.Panel("contract-changes", "Changes"); scroll.Add(changes);
            var generationPanel = Controls.Panel("contract-generation"); scroll.Add(generationPanel);
            generation = new DeucarianEditorWorkspaceForm(generationPanel).Section("Generation settings", true);
            var path = generation.Text("contract-source", "OpenAPI file", () => owner.SpecPath,
                value => owner.ChangeSource(value, owner.Revision));
            path.isDelayed = true;
            path.parent.Add(Controls.IconButton("Browse", DeucarianEditorIconIds.Folder, owner.Browse));
            var revision = generation.Text("contract-revision", "Backend commit", () => owner.Revision,
                value => owner.ChangeSource(owner.SpecPath, value)); revision.isDelayed = true;
            generation.Note(() => "Choose a local OpenAPI file and the hexadecimal Git commit that generated it.");
            generation.ReadOnly(null, "Operations", () => owner.Current?.catalog == null ? "Unknown" :
                owner.Current.catalog.operationCount + " total · " + owner.Current.catalog.reviewedStableOperationCount + " stable");
            generation.ReadOnly(null, "SHA-256", () => owner.Current?.source?.sha256 ?? "Unknown");
            generation.ReadOnly(null, "OpenAPI", () => owner.Current?.source?.openapiVersion ?? "Unknown");
            generation.Note(() => SimultriaContractUpdateService.IsEditablePackage
                ? "Preview first. Updates change generated files in this package checkout, ready for your review."
                : "This installed package is read-only. Use a local checkout to apply generated updates.");
            apply = Controls.Button("Update contract", owner.RunApply, true); apply.name = "contract-apply";
            scroll.Add(Controls.EndActions(apply));
            Refresh();
        }

        internal void Refresh()
        {
            versions.Refresh(); generation.Refresh();
            check.SetEnabled(owner.CanPreview);
            check.tooltip = owner.CanPreview ? "Preview the local incoming file. No backend request is made."
                : "Select a local OpenAPI file and its backend commit in Generation settings.";
            apply.SetEnabled(owner.HasPreview && SimultriaContractUpdateService.IsEditablePackage);
            changes.Clear(); changes.Add(Controls.Label("Changes", "dw-section-title"));
            var result = owner.LastResult;
            if (result == null)
            {
                var empty = Controls.Region(null, "dw-empty-preview");
                empty.Add(Controls.Icon("file-text")); empty.Add(Controls.Label("No changes loaded", "dw-muted")); changes.Add(empty);
                return;
            }
            var summary = new DeucarianEditorStatusSummary("contract-result");
            summary.Set(result.Succeeded ? "Preview ready" : "Preview failed", result.Message,
                result.Succeeded ? SimultriaContractChangeView.GetStatus(result.ChangeReport) : DeucarianEditorStatus.Error);
            changes.Add(summary.Root);
            var form = new DeucarianEditorWorkspaceForm(changes);
            if (!result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(result.ProcessOutput))
                    form.Section("Process details", true).ReadOnly(null, "Output", () => result.ProcessOutput);
                return;
            }
            var report = result.ChangeReport;
            if (report?.summary != null)
                form.ReadOnly(null, "Summary", () => report.summary.added + " added · " + report.summary.changed + " changed · " + report.summary.removed + " removed");
            if (report?.breakingOrSecurityReviewRequired == true)
                form.Note(() => "Breaking or security-sensitive changes need review before merging.");
            if (report != null) SimultriaContractChangeView.BuildDetails(form.Section("Endpoint changes", true), report);
        }
        private static string Short(string value, string fallback) => string.IsNullOrWhiteSpace(value)
            ? fallback : value.Length <= 16 ? value : value.Substring(0, 16) + "…";
        public void Dispose() => workspace.Dispose();
    }
}
