using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Generated;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API
{
    public static partial class SimultriaEndpoints
    {
        public static partial class Projects
        {
            #region Properties
            /// <summary>The generated project endpoint with the documented model-selection field projection.</summary>
            public static SimultriaOperation<ProjectsGetProjectRequest, SimultriaResourceResponse<SimultriaProjectDto>>
                GetModelTree =>
                    new SimultriaOperation<ProjectsGetProjectRequest, SimultriaResourceResponse<SimultriaProjectDto>>(
                        GetProject.EndpointId.Value,
                        GetProject.RouteTemplate,
                        CreateModelTreeRequest
                    );
            #endregion

            #region Methods
            private static ApiRequest CreateModelTreeRequest(ApiEndpoint endpoint, ProjectsGetProjectRequest parameters)
            {
                ApiRequest request = GetProject.CreateRequest(endpoint, parameters);
                // BaseResource.php supports a recursive field projection. Keep these wire names at this boundary.
                // The full OpenAPI response includes unrelated fields whose schemas are still incomplete.
                request.QueryParameters["included_fields"] =
                    "projects.id,projects.name,projects.models,projects.sub_projects,"
                    + "models.id,models.name,models.active_version,models.model_versions,models.versions,"
                    + "model_versions.id,model_versions.name,model_versions.download_link,model_versions.version,"
                    + "model_versions.version_number,model_versions.order,model_versions.updated_at,model_versions.created_at";
                return request;
            }
            #endregion
        }
    }

    public sealed partial class ProjectsOperations
    {
        #region Methods
        public Task<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>> GetModelTreeAsync(
            ProjectsGetProjectRequest parameters,
            CancellationToken cancellationToken = default) =>
            _api.SendAsync(SimultriaEndpoints.Projects.GetModelTree, parameters, cancellationToken);

        /// <summary>Cancellation calls onFailure with IsCancellation set; completion runs on the Unity main thread.</summary>
        public void GetModelTree(ProjectsGetProjectRequest parameters,
                                 Action<SimultriaResourceResponse<SimultriaProjectDto>> onSuccess,
                                 Action<ApiError> onFailure,
                                 CancellationToken cancellationToken = default) =>
            _api.Send(SimultriaEndpoints.Projects.GetModelTree, parameters, onSuccess, onFailure, cancellationToken);

        /// <summary>Get projects, child projects, models and versions needed for model selection.</summary>
        public Task<ApiResult<SimultriaResourceResponse<SimultriaProjectDto>>> GetModelTreeAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            _api.SendAsync(SimultriaEndpoints.Projects.GetModelTree,
                           new ProjectsGetProjectRequest { Id = projectId }, cancellationToken
            );

        /// <summary>Get the model tree with exactly one completion on the Unity main thread.</summary>
        /// <remarks>Cancellation calls onFailure with ApiError.IsCancellation set.</remarks>
        public void GetModelTree(int projectId,
                                 Action<SimultriaResourceResponse<SimultriaProjectDto>> onSuccess,
                                 Action<ApiError> onFailure,
                                 CancellationToken cancellationToken = default) =>
            _api.Send(SimultriaEndpoints.Projects.GetModelTree,
                      new ProjectsGetProjectRequest { Id = projectId }, onSuccess, onFailure, cancellationToken
            );
        #endregion
    }
}
