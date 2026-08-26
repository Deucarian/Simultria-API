using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    [InitializeOnLoad]
    internal static class SimultriaContractInboxNotifier
    {
        private const string NotifiedHashSessionKey =
            "Deucarian.Simultria.API.Contract.NotifiedHash";

        static SimultriaContractInboxNotifier()
        {
            EditorApplication.delayCall += NotifyWhenIncomingSpecChanged;
        }

        internal static bool ShouldNotify(
            string specPath,
            SimultriaContractManifestDocument currentManifest,
            string alreadyNotifiedHash,
            out string incomingHash)
        {
            incomingHash = string.Empty;
            if (!SimultriaContractUpdateService.IsEditablePackage ||
                string.IsNullOrWhiteSpace(specPath) ||
                !File.Exists(specPath) ||
                !SimultriaContractUpdateService.HasIncomingContractChange(
                    specPath,
                    currentManifest))
            {
                return false;
            }

            incomingHash = SimultriaContractUpdateService.ComputeSha256(
                specPath);
            return !string.Equals(
                incomingHash,
                alreadyNotifiedHash,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void NotifyWhenIncomingSpecChanged()
        {
            string specPath =
                SimultriaContractUpdateService.DefaultIncomingSpecPath;
            SimultriaContractUpdateService.TryLoadCurrentManifest(
                out SimultriaContractManifestDocument manifest,
                out _);
            string alreadyNotified = SessionState.GetString(
                NotifiedHashSessionKey,
                string.Empty);
            if (!ShouldNotify(
                    specPath,
                    manifest,
                    alreadyNotified,
                    out string incomingHash))
            {
                return;
            }

            SessionState.SetString(NotifiedHashSessionKey, incomingHash);
            SimultriaContractUpdateWindow window =
                SimultriaContractUpdateWindow.OpenWindow();
            window.UseIncomingSpec(specPath);
            window.ShowNotification(
                new GUIContent(
                    "A new Simultria OpenAPI file is ready to preview."));
        }
    }
}
