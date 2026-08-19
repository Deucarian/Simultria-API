using System;
using System.Collections.Generic;
using System.IO;
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
        private bool showAdvanced;

        public override void OnInspectorGUI()
        {
            var profile = (SimultriaApiProfile)target;
            bool projectOwned = IsProjectOwned(profile);
            ApiEndpointCatalog packageCatalog =
                AssetDatabase.LoadAssetAtPath<ApiEndpointCatalog>(
                    SimultriaApiProfileDefaults
                        .DefaultEndpointCatalogAssetPath);
            bool usesPackageCatalog = packageCatalog != null &&
                profile.EndpointCatalog == packageCatalog;

            EditorGUILayout.LabelField(
                "Simultria API Profile",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Configure the API host for each environment this project may " +
                "use. Blank environments stay disabled.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(3f);

            DrawContractSummary(profile, usesPackageCatalog);

            if (!projectOwned)
            {
                EditorGUILayout.HelpBox(
                    "This package fallback is read-only. Create a project " +
                    "profile to enter environment URLs.",
                    MessageType.Info);
                if (GUILayout.Button("Create Project Profile"))
                {
                    SimultriaApiProfileAssetFactory.CreateFromMenu();
                }
            }

            EditorGUILayout.Space(4f);
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                DrawEnvironmentCard(profile, descriptor, projectOwned);
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(2f);
            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced · policies and contract overrides",
                true);
            if (showAdvanced)
            {
                DrawAdvanced(profile, projectOwned, packageCatalog);
            }
        }

        private static void DrawContractSummary(
            SimultriaApiProfile profile,
            bool usesPackageCatalog)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    usesPackageCatalog
                        ? "Simultria API v2 · package managed · read-only"
                        : "Simultria API v2 · project override",
                    EditorStyles.boldLabel);
                int endpointCount = profile.EndpointCatalog != null &&
                    profile.EndpointCatalog.Endpoints != null
                        ? profile.EndpointCatalog.Endpoints.Count
                        : 0;
                EditorGUILayout.LabelField(
                    endpointCount +
                    " contract operations · deployment URLs stay project owned",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawEnvironmentCard(
            SimultriaApiProfile profile,
            ApiEnvironmentDescriptor descriptor,
            bool projectOwned)
        {
            ApiEnvironmentProfile environment = FindEnvironment(
                profile.Environments,
                descriptor.EnvironmentId);
            ApiNamedClientDefinition client = null;
            ApiEnvironmentProfileConfigurationState state =
                ApiEnvironmentProfileConfigurationState.NotConfigured;
            string stateMessage = null;

            if (environment == null)
            {
                state = ApiEnvironmentProfileConfigurationState.Invalid;
                stateMessage = descriptor.DisplayName +
                    " has no configuration slot.";
            }
            else if (!environment.TryGetClient(
                SimultriaClientIds.Primary,
                out client))
            {
                state = ApiEnvironmentProfileConfigurationState.Invalid;
                stateMessage = descriptor.DisplayName +
                    " does not define the required primary API client.";
            }
            else
            {
                state = environment.ClassifyConfiguration(out stateMessage);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        descriptor.DisplayName,
                        EditorStyles.boldLabel);
                    DrawStateBadge(state);
                }

                using (new EditorGUI.DisabledScope(
                    !projectOwned ||
                    client == null ||
                    !IsProjectOwned(environment)))
                {
                    EditorGUI.BeginChangeCheck();
                    string baseUrl = EditorGUILayout.TextField(
                        "Base URL",
                        client?.BaseUrl ?? string.Empty);
                    if (EditorGUI.EndChangeCheck() && client != null)
                    {
                        Undo.RecordObject(
                            environment,
                            "Configure Simultria API environment");
                        client.BaseUrl = baseUrl;
                        EditorUtility.SetDirty(environment);
                    }
                }

                if (state == ApiEnvironmentProfileConfigurationState.Invalid)
                {
                    EditorGUILayout.HelpBox(
                        stateMessage ?? "This environment is invalid.",
                        MessageType.Error);
                }
            }
        }

        private static void DrawStateBadge(
            ApiEnvironmentProfileConfigurationState state)
        {
            switch (state)
            {
                case ApiEnvironmentProfileConfigurationState.Configured:
                    DeucarianEditorStatusBadge.Draw(
                        "Configured",
                        DeucarianEditorStatus.Success,
                        GUILayout.Width(112f));
                    break;
                case ApiEnvironmentProfileConfigurationState.NotConfigured:
                    DeucarianEditorStatusBadge.Draw(
                        "Not configured",
                        DeucarianEditorStatus.Warning,
                        GUILayout.Width(112f));
                    break;
                default:
                    DeucarianEditorStatusBadge.Draw(
                        "Invalid",
                        DeucarianEditorStatus.Error,
                        GUILayout.Width(112f));
                    break;
            }
        }

        private void DrawAdvanced(
            SimultriaApiProfile profile,
            bool projectOwned,
            ApiEndpointCatalog packageCatalog)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.HelpBox(
                "Advanced changes belong to this project. The package contract " +
                "asset is never edited in place.",
                MessageType.Info);

            DrawCatalogOverride(profile, projectOwned, packageCatalog);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Environment details and policies",
                EditorStyles.boldLabel);
            foreach (ApiEnvironmentDescriptor descriptor in
                SimultriaEnvironmentDescriptors.Standard)
            {
                ApiEnvironmentProfile environment = FindEnvironment(
                    profile.Environments,
                    descriptor.EnvironmentId);
                DrawEnvironmentAdvanced(
                    environment,
                    descriptor,
                    projectOwned);
            }

            int additionalCount = CountAdditionalEnvironments(profile);
            if (additionalCount > 0)
            {
                EditorGUILayout.HelpBox(
                    additionalCount +
                    " additional custom environment profile(s) are attached. " +
                    "Select their sub-assets for detailed editing.",
                    MessageType.Info);
            }
        }

        private void DrawCatalogOverride(
            SimultriaApiProfile profile,
            bool projectOwned,
            ApiEndpointCatalog packageCatalog)
        {
            EditorGUILayout.LabelField("API contract", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!projectOwned))
            {
                EditorGUI.BeginChangeCheck();
                var selected = (ApiEndpointCatalog)EditorGUILayout.ObjectField(
                    "Endpoint catalog",
                    profile.EndpointCatalog,
                    typeof(ApiEndpointCatalog),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!SimultriaApiProfileAssetFactory.TryAssignEndpointCatalog(
                        profile,
                        selected,
                        out string assignError))
                    {
                        EditorUtility.DisplayDialog(
                            "Select Simultria API Contract",
                            assignError,
                            "OK");
                    }
                }
            }

            ApiEndpointCatalog selectedCatalog = profile.EndpointCatalog;
            if (selectedCatalog != null)
            {
                EditorGUILayout.LabelField(
                    "Catalog ID",
                    selectedCatalog.CatalogId ?? string.Empty);
                EditorGUILayout.LabelField(
                    "Operations",
                    selectedCatalog.Endpoints.Count.ToString());
            }

            if (!projectOwned)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (profile.EndpointCatalog == packageCatalog)
                {
                    if (GUILayout.Button("Create Project Override…"))
                    {
                        CreateCatalogOverride(profile);
                    }
                }
                else if (GUILayout.Button("Use Package Contract"))
                {
                    if (!SimultriaApiProfileAssetFactory
                        .TryAssignEndpointCatalog(
                            profile,
                            packageCatalog,
                            out string resetError))
                    {
                        EditorUtility.DisplayDialog(
                            "Use Package Contract",
                            resetError,
                            "OK");
                    }
                }

                if (profile.EndpointCatalog != null &&
                    GUILayout.Button("Select Contract Asset"))
                {
                    Selection.activeObject = profile.EndpointCatalog;
                    EditorGUIUtility.PingObject(profile.EndpointCatalog);
                }
            }
        }

        private static void CreateCatalogOverride(SimultriaApiProfile profile)
        {
            string profilePath = AssetDatabase.GetAssetPath(profile)
                ?.Replace('\\', '/');
            string directory = !string.IsNullOrWhiteSpace(profilePath)
                ? Path.GetDirectoryName(profilePath)?.Replace('\\', '/')
                : "Assets";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Simultria API Contract Override",
                "SimultriaApiV2EndpointCatalog.Override",
                "asset",
                "Choose where this project-owned contract override should live.",
                directory ?? "Assets");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!SimultriaApiProfileAssetFactory
                .TryCreateProjectCatalogOverride(
                    profile,
                    path,
                    out ApiEndpointCatalog endpointCatalog,
                    out string error))
            {
                EditorUtility.DisplayDialog(
                    "Create Simultria API Contract Override",
                    error,
                    "OK");
                return;
            }

            Selection.activeObject = endpointCatalog;
            EditorGUIUtility.PingObject(endpointCatalog);
        }

        private static void DrawEnvironmentAdvanced(
            ApiEnvironmentProfile environment,
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
                EditorGUILayout.LabelField("Stage", descriptor.Stage.ToString());

                if (environment == null)
                {
                    EditorGUILayout.LabelField(
                        "No configuration sub-asset is attached.",
                        EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Environment asset",
                        environment,
                        typeof(ApiEnvironmentProfile),
                        false);
                    EditorGUILayout.TextField(
                        "Client ID",
                        SimultriaClientIds.Primary.Value);
                }

                var environmentObject = new SerializedObject(environment);
                environmentObject.Update();
                bool environmentProjectOwned =
                    projectOwned && IsProjectOwned(environment);
                using (new EditorGUI.DisabledScope(!environmentProjectOwned))
                {
                    EditorGUILayout.PropertyField(
                        environmentObject.FindProperty("defaultRequestPolicy"),
                        new GUIContent("Environment policy"),
                        true);

                    SerializedProperty clients =
                        environmentObject.FindProperty("clients");
                    SerializedProperty primaryClient = FindPrimaryClient(
                        clients);
                    if (primaryClient != null)
                    {
                        EditorGUILayout.PropertyField(
                            primaryClient.FindPropertyRelative("defaultHeaders"),
                            new GUIContent("Default headers"),
                            true);
                        EditorGUILayout.PropertyField(
                            primaryClient.FindPropertyRelative("requestPolicy"),
                            new GUIContent("Client policy"),
                            true);
                    }
                }

                environmentObject.ApplyModifiedProperties();
            }
        }

        private static SerializedProperty FindPrimaryClient(
            SerializedProperty clients)
        {
            if (clients == null || !clients.isArray)
            {
                return null;
            }

            for (int index = 0; index < clients.arraySize; index++)
            {
                SerializedProperty candidate =
                    clients.GetArrayElementAtIndex(index);
                SerializedProperty id =
                    candidate.FindPropertyRelative("clientId");
                if (id != null && string.Equals(
                    id.stringValue,
                    SimultriaClientIds.Primary.Value,
                    StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsProjectOwned(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset)?.Replace('\\', '/');
            return !string.IsNullOrWhiteSpace(path) &&
                path.StartsWith("Assets/", StringComparison.Ordinal);
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
