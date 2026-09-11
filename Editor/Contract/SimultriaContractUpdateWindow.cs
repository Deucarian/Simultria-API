using System;
using System.IO;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Simultria.API.Editor
{
    internal sealed class SimultriaContractUpdateWindow : EditorWindow
    {
        private const string SpecPathPreference = "Deucarian.Simultria.API.Contract.SpecPath";
        private const string RevisionPreference = "Deucarian.Simultria.API.Contract.BackendRevision";
        private string specPath, sourceRevision, previewSpecHash, previewRevision;
        private SimultriaContractWorkspace view;
        private DeucarianEditorPageSession navigation;
        internal SimultriaContractManifestDocument Current { get; private set; }
        internal SimultriaContractUpdateResult LastResult { get; private set; }
        internal string SpecPath => specPath;
        internal string Revision => sourceRevision;
        internal bool CanPreview => File.Exists(specPath ?? string.Empty) && SimultriaContractUpdateService.IsValidSourceRevision(sourceRevision);
        internal bool HasPreview => LastResult?.Succeeded == true && !string.IsNullOrEmpty(previewSpecHash);

        internal static SimultriaContractUpdateWindow OpenWindow() =>
            DeucarianEditorWindowPages.ShowStandalone<SimultriaContractUpdateWindow>("API contract", new Vector2(620, 540));

        internal void UseIncomingSpec(string path)
        {
            ChangeSource(path, sourceRevision);
            if (!DeucarianEditorWindowPages.IsPageController(this) && navigation != null)
                navigation.Navigate(DeucarianToolIds.SimultriaContractUpdater, path);
        }

        internal void ChangeSource(string path, string revision)
        {
            specPath = path; sourceRevision = revision;
            ClearPreview(); SavePreferences(); view?.Refresh();
        }

        private void OnEnable()
        {
            specPath = EditorPrefs.GetString(SpecPathPreference, SimultriaContractUpdateService.DefaultIncomingSpecPath);
            sourceRevision = EditorPrefs.GetString(RevisionPreference, string.Empty);
            ReloadCurrent();
        }
        private void OnDisable()
        {
            navigation?.Dispose(); navigation = null;
            view?.Dispose(); view = null;
        }
        private void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, "contract-home", _ => { });
            navigation.Navigate(DeucarianToolIds.SimultriaContractUpdater);
        }
        public static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorWindowPages.Create<SimultriaContractUpdateWindow>(
                (window, root) => window.view = new SimultriaContractWorkspace(root, window),
                (window, route) => { if (!string.IsNullOrEmpty(route)) window.UseIncomingSpec(route); window.view.Refresh(); });

        internal void Browse()
        {
            string directory = SimultriaContractUpdateService.PackageRoot ?? string.Empty;
            try { if (File.Exists(specPath)) directory = Path.GetDirectoryName(Path.GetFullPath(specPath)); }
            catch (ArgumentException) { }
            string selected = EditorUtility.OpenFilePanel("Select backend OpenAPI contract", directory, string.Empty);
            if (!string.IsNullOrWhiteSpace(selected)) ChangeSource(selected, sourceRevision);
        }

        internal void RunPreview()
        {
            if (!CanPreview) return;
            ClearPreview();
            Execute(false);
        }
        internal void RunApply()
        {
            if (!SimultriaContractUpdateService.IsEditablePackage || !IsPreviewCurrent())
            { ClearPreview(); view?.Refresh(); return; }
            string warning = LastResult.ChangeReport?.breakingOrSecurityReviewRequired == true
                ? "Breaking or security-sensitive endpoint changes were detected. Apply the generated files for review?"
                : "Apply all generated contract files to the package checkout?";
            if (!EditorUtility.DisplayDialog("Apply Simultria API contract update", warning, "Apply for review", "Cancel")) return;
            if (!IsPreviewCurrent()) { ClearPreview(); view?.Refresh(); return; }
            Execute(true);
        }
        private void Execute(bool apply)
        {
            try
            {
                LastResult = apply ? SimultriaContractUpdateService.Apply(specPath, sourceRevision)
                    : SimultriaContractUpdateService.Preview(specPath, sourceRevision);
                if (LastResult.Succeeded)
                {
                    previewSpecHash = SimultriaContractUpdateService.ComputeSha256(specPath);
                    previewRevision = sourceRevision.Trim().ToLowerInvariant();
                    if (apply) ReloadCurrent();
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                ClearPreview();
                LastResult = new SimultriaContractUpdateResult { Succeeded = false, Message = "The local contract could not be read. Check its path and access." };
            }
            view?.Refresh();
        }
        private bool IsPreviewCurrent()
        {
            if (!HasPreview || !CanPreview) return false;
            try
            {
                return string.Equals(previewSpecHash, SimultriaContractUpdateService.ComputeSha256(specPath), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(previewRevision, sourceRevision.Trim().ToLowerInvariant(), StringComparison.Ordinal);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        private void ReloadCurrent()
        {
            SimultriaContractUpdateService.TryLoadCurrentManifest(out var manifest, out _);
            Current = manifest;
        }
        private void ClearPreview() { LastResult = null; previewSpecHash = null; previewRevision = null; }
        private void SavePreferences()
        {
            EditorPrefs.SetString(SpecPathPreference, specPath ?? string.Empty);
            EditorPrefs.SetString(RevisionPreference, sourceRevision ?? string.Empty);
        }
    }
}
