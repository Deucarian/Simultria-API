using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Simultria.API.Editor
{
    /// <summary>Stable editor entry points for Simultria API developer tools.</summary>
    public static class SimultriaApiEditorTools
    {
        public static void OpenContractUpdater()
        {
            SimultriaContractUpdateWindow.OpenWindow();
        }

        public static void OpenDocumentation()
        {
            SimultriaApiDocumentationMenu.OpenDocumentation();
        }

        public static void OpenEndpointReference()
        {
            SimultriaApiDocumentationMenu.OpenEndpointReference();
        }
    }

    [InitializeOnLoad]
    internal static class SimultriaApiControlCenterRegistration
    {
        private const string PackageId = "com.deucarian.simultria-api";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;

        static SimultriaApiControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.SimultriaContractUpdater,
                    "Simultria API Contract",
                    "Review and update the local generated Simultria API contract.",
                    DeucarianControlCenterArea.Developer,
                    SimultriaApiEditorTools.OpenContractUpdater,
                    PackageId,
                    searchTerms: new[] { "simultria", "api", "contract", "endpoints" },
                    order: 100));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new SimultriaApiDeveloperCardProvider());
        }
    }

    internal sealed class SimultriaApiDeveloperCardProvider :
        IDeucarianControlCenterCardProvider
    {
        private const string PackageId = "com.deucarian.simultria-api";

        public string Id => PackageId + ".control-center";

        public IEnumerable<DeucarianControlCenterCard> Capture(
            DeucarianControlCenterContext context)
        {
            bool loaded = SimultriaContractUpdateService.TryLoadCurrentManifest(
                out SimultriaContractManifestDocument manifest,
                out _);
            bool valid = loaded && manifest?.source != null &&
                manifest.catalog != null && manifest.coverage != null;
            bool incomingChange = valid &&
                SimultriaContractUpdateService.HasIncomingContractChange(
                    SimultriaContractUpdateService.DefaultIncomingSpecPath,
                    manifest);

            yield return CreateCard(new SimultriaApiContractSnapshot(
                valid,
                valid && manifest.coverage.snapshotCoverageComplete,
                incomingChange,
                valid ? manifest.catalog.operationCount : 0,
                valid ? manifest.source.backendRevision : null));
        }

        internal static DeucarianControlCenterCard CreateCard(
            SimultriaApiContractSnapshot snapshot)
        {
            DeucarianControlCenterStatus status = !snapshot.ManifestValid ||
                !snapshot.CoverageComplete
                    ? DeucarianControlCenterStatus.Error
                    : snapshot.IncomingChange
                        ? DeucarianControlCenterStatus.Warning
                        : DeucarianControlCenterStatus.Success;
            string statusText = !snapshot.ManifestValid
                ? "Contract manifest unavailable"
                : !snapshot.CoverageComplete
                    ? "Contract coverage incomplete"
                    : snapshot.IncomingChange
                        ? "Incoming contract review required"
                        : "Pinned contract current";

            return new DeucarianControlCenterCard(
                PackageId + ".developer-tools",
                DeucarianControlCenterArea.Developer,
                "Simultria API",
                "Pinned local contract provenance, coverage, and review tools.",
                PackageId,
                status,
                statusText,
                order: 100,
                details: new[]
                {
                    "Backend revision: " + snapshot.RevisionSummary,
                    "Catalog operations: " + snapshot.OperationCount,
                    snapshot.CoverageComplete
                        ? "Snapshot coverage: complete"
                        : "Snapshot coverage: incomplete",
                    snapshot.IncomingChange
                        ? "Incoming contract: review required"
                        : "Incoming contract: no local drift"
                },
                actions: new[]
                {
                    new DeucarianControlCenterAction(
                        PackageId + ".open-updater",
                        "Open Contract Updater",
                        SimultriaApiEditorTools.OpenContractUpdater),
                    new DeucarianControlCenterAction(
                        PackageId + ".open-docs",
                        "Open Documentation",
                        SimultriaApiEditorTools.OpenDocumentation),
                    new DeucarianControlCenterAction(
                        PackageId + ".open-endpoints",
                        "Open Endpoint Reference",
                        SimultriaApiEditorTools.OpenEndpointReference)
                },
                searchTerms: new[]
                {
                    "simultria", "api", "contract", "documentation",
                    "coverage", "drift", "provenance"
                });
        }
    }

    internal sealed class SimultriaApiContractSnapshot
    {
        internal SimultriaApiContractSnapshot(
            bool manifestValid,
            bool coverageComplete,
            bool incomingChange,
            int operationCount,
            string backendRevision)
        {
            ManifestValid = manifestValid;
            CoverageComplete = coverageComplete;
            IncomingChange = incomingChange;
            OperationCount = Math.Max(0, operationCount);
            string revision = string.IsNullOrWhiteSpace(backendRevision)
                ? "not recorded"
                : backendRevision.Trim();
            RevisionSummary = revision.Length <= 16
                ? revision
                : revision.Substring(0, 16) + "…";
        }

        internal bool ManifestValid { get; }
        internal bool CoverageComplete { get; }
        internal bool IncomingChange { get; }
        internal int OperationCount { get; }
        internal string RevisionSummary { get; }
    }
}