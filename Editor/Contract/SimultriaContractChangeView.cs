using System;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaContractChangeView
    {
        internal static DeucarianEditorStatus GetStatus(
            SimultriaContractChangeReport report)
        {
            if (report == null || !report.reviewRequired)
            {
                return DeucarianEditorStatus.Success;
            }

            return report.breakingOrSecurityReviewRequired
                ? DeucarianEditorStatus.Warning
                : DeucarianEditorStatus.Info;
        }

        internal static void DrawDetails(
            SimultriaContractChangeReport report)
        {
            if (report == null)
            {
                return;
            }

            DrawEndpointChanges("Added", report.added);
            DrawEndpointChanges("Removed", report.removed);
            DrawChangedEndpoints(report.changed);
        }

        private static void DrawEndpointChanges(
            string heading,
            SimultriaContractEndpointChange[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);
            int visibleCount = Math.Min(endpoints.Length, 12);
            for (int index = 0; index < visibleCount; index++)
            {
                SimultriaContractEndpointChange endpoint = endpoints[index];
                EditorGUILayout.LabelField(
                    endpoint.method + " " + endpoint.routeTemplate,
                    endpoint.endpointId,
                    EditorStyles.miniLabel);
            }

            DrawRemainingCount(endpoints.Length, visibleCount);
        }

        private static void DrawChangedEndpoints(
            SimultriaContractChangedEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField("Changed", EditorStyles.boldLabel);
            int visibleCount = Math.Min(endpoints.Length, 12);
            for (int index = 0; index < visibleCount; index++)
            {
                SimultriaContractChangedEndpoint endpoint = endpoints[index];
                string fields = endpoint.changes == null
                    ? string.Empty
                    : string.Join(
                        ", ",
                        Array.ConvertAll(
                            endpoint.changes,
                            change => change.field));
                EditorGUILayout.LabelField(
                    endpoint.endpointId,
                    fields,
                    EditorStyles.miniLabel);
            }

            DrawRemainingCount(endpoints.Length, visibleCount);
        }

        private static void DrawRemainingCount(int total, int visible)
        {
            if (visible >= total)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "+ " + (total - visible) +
                " more in the generated change report",
                EditorStyles.miniLabel);
        }
    }
}
