using System;
using System.Linq;
using Deucarian.Editor;

namespace Deucarian.Simultria.API.Editor
{
    internal static class SimultriaContractChangeView
    {
        internal static DeucarianEditorStatus GetStatus(SimultriaContractChangeReport report) =>
            report == null || !report.reviewRequired ? DeucarianEditorStatus.Success :
            report.breakingOrSecurityReviewRequired ? DeucarianEditorStatus.Warning : DeucarianEditorStatus.Info;

        internal static void BuildDetails(DeucarianEditorWorkspaceForm form, SimultriaContractChangeReport report)
        {
            AddEndpoints(form, "Added", report.added);
            AddEndpoints(form, "Removed", report.removed);
            var changed = report.changed ?? Array.Empty<SimultriaContractChangedEndpoint>();
            if (changed.Length == 0) return;
            var section = form.Section("Changed", true);
            foreach (var endpoint in changed)
                section.ReadOnly(null, endpoint.endpointId, () =>
                    string.Join(", ", (endpoint.changes ?? Array.Empty<SimultriaContractFieldChange>()).Select(value => value.field)));
        }
        private static void AddEndpoints(DeucarianEditorWorkspaceForm form, string title, SimultriaContractEndpointChange[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0) return;
            var section = form.Section(title, true);
            foreach (var endpoint in endpoints)
                section.ReadOnly(null, endpoint.method + " " + endpoint.routeTemplate, () => endpoint.endpointId);
        }
    }
}
