using System;
using System.Collections.Generic;
using Deucarian.API.Configuration;
using Deucarian.API.Models;
using Deucarian.Editor;
using Deucarian.Simultria.API.Configuration;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Simultria.API.Editor
{
    [CustomEditor(typeof(SimultriaApiProfile))]
    internal sealed class SimultriaApiProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var profile = (SimultriaApiProfile)target;
            string assetPath = AssetDatabase.GetAssetPath(profile)
                ?.Replace('\\', '/');
            bool projectOwned = !string.IsNullOrWhiteSpace(assetPath) &&
                assetPath.StartsWith("Assets/", StringComparison.Ordinal);

            EditorGUILayout.LabelField(
                "Simultria API Profile",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Known environments stay credential-free. Enter a host only " +
                "for environments this project is allowed to use.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Endpoint catalog",
                    profile.EndpointCatalog,
                    typeof(ApiEndpointCatalog),
                    false);
            }

            if (!projectOwned)
            {
                EditorGUILayout.HelpBox(
                    "This package fallback profile is read-only. Create " +
                    "a project-owned profile from Assets > Create > Deucarian " +
                    "> Simultria > API Profile to configure environments.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                DrawEnvironment(profile, descriptor, projectOwned);
                EditorGUILayout.Space(2f);
            }

            int additionalCount = CountAdditionalEnvironments(profile);
            if (additionalCount > 0)
            {
                EditorGUILayout.HelpBox(
                    additionalCount +
                    " additional custom environment profile(s) are attached. " +
                    "Select their sub-assets to inspect them.",
                    MessageType.Info);
            }
        }

        private static void DrawEnvironment(
            SimultriaApiProfile profile,
            ApiEnvironmentDescriptor descriptor,
            bool projectOwned)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    descriptor.DisplayName,
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Environment ID",
                    descriptor.EnvironmentId.Value);
                EditorGUILayout.LabelField(
                    "Stage",
                    descriptor.Stage.ToString());

                ApiEnvironmentProfile environment = FindEnvironment(
                    profile.Environments,
                    descriptor.EnvironmentId);
                if (environment == null)
                {
                    DrawState(
                        "Not configured",
                        descriptor.DisplayName +
                        " has no project-owned configuration slot.",
                        DeucarianEditorStatus.Warning,
                        MessageType.Info);
                    return;
                }

                if (!environment.TryGetClient(
                        SimultriaClientIds.Primary,
                        out ApiNamedClientDefinition client))
                {
                    DrawState(
                        "Invalid",
                        descriptor.DisplayName +
                        " does not define the required primary API client.",
                        DeucarianEditorStatus.Error,
                        MessageType.Error);
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        "Client ID",
                        SimultriaClientIds.Primary.Value);
                }

                using (new EditorGUI.DisabledScope(!projectOwned))
                {
                    EditorGUI.BeginChangeCheck();
                    string baseUrl = EditorGUILayout.TextField(
                        "Base URL",
                        client.BaseUrl ?? string.Empty);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(
                            environment,
                            "Configure Simultria API environment");
                        client.BaseUrl = baseUrl;
                        EditorUtility.SetDirty(environment);
                    }
                }

                ApiEnvironmentProfileConfigurationState state =
                    environment.ClassifyConfiguration(out string message);
                switch (state)
                {
                    case ApiEnvironmentProfileConfigurationState.Configured:
                        DrawState(
                            "Configured",
                            descriptor.DisplayName +
                            " has a valid API host.",
                            DeucarianEditorStatus.Success,
                            MessageType.Info);
                        break;
                    case ApiEnvironmentProfileConfigurationState.NotConfigured:
                        DrawState(
                            "Not configured",
                            descriptor.DisplayName +
                            " is disabled until an absolute HTTP(S) base URL " +
                            "is entered.",
                            DeucarianEditorStatus.Warning,
                            MessageType.Info);
                        break;
                    default:
                        DrawState(
                            "Invalid",
                            message ??
                            descriptor.DisplayName +
                            " contains an invalid API configuration.",
                            DeucarianEditorStatus.Error,
                            MessageType.Error);
                        break;
                }
            }
        }

        private static void DrawState(
            string label,
            string message,
            DeucarianEditorStatus status,
            MessageType messageType)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Status", GUILayout.Width(116f));
                DeucarianEditorStatusBadge.Draw(
                    label,
                    status,
                    GUILayout.Width(112f));
            }
            EditorGUILayout.HelpBox(message, messageType);
        }

        private static ApiEnvironmentProfile FindEnvironment(
            IReadOnlyList<ApiEnvironmentProfile> environments,
            ApiEnvironmentId environmentId)
        {
            if (environments != null)
            {
                foreach (ApiEnvironmentProfile environment in environments)
                {
                    if (environment != null &&
                        environment.TryGetId(out ApiEnvironmentId candidate) &&
                        candidate == environmentId)
                    {
                        return environment;
                    }
                }
            }

            return null;
        }

        private static int CountAdditionalEnvironments(
            SimultriaApiProfile profile)
        {
            int count = 0;
            foreach (ApiEnvironmentProfile environment in profile.Environments)
            {
                if (environment == null ||
                    !environment.TryGetId(out ApiEnvironmentId environmentId) ||
                    FindDescriptor(environmentId) == null)
                {
                    count++;
                }
            }

            return count;
        }

        private static ApiEnvironmentDescriptor FindDescriptor(
            ApiEnvironmentId environmentId)
        {
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                if (descriptor.EnvironmentId == environmentId)
                {
                    return descriptor;
                }
            }

            return null;
        }
    }
}
