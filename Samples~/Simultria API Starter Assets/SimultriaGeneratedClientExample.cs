using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Generated;

namespace Deucarian.Simultria.API.Samples.StarterAssets
{
    /// <summary>
    /// Receive the connection-owned API through constructor injection. No global access or repeated setup is needed.
    /// Use autocomplete on the API groups or Go to Definition on SimultriaEndpoints to browse the contract.
    /// Regenerate the client with Tools~/update_contract.py when importing a new backend OpenAPI snapshot.
    /// </summary>
    public sealed class SimultriaGeneratedClientExample
    {
        #region Fields
        private readonly SimultriaApi _api;
        #endregion

        #region Constructors
        public SimultriaGeneratedClientExample(SimultriaApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }
        #endregion

        #region Methods
        public void LoadActivities(int modelVersionId,
                                   Action<ModelVersionsGetActivitiesResponse> onSuccess,
                                   Action<ApiError> onFailure,
                                   CancellationToken cancellationToken = default)
        {
            _api.ModelVersions.GetActivities(modelVersionId, onSuccess, onFailure, cancellationToken);
        }

        public void LoadProjects(Action<ProjectsGetProjectsResponse> onSuccess,
                                 Action<ApiError> onFailure,
                                 CancellationToken cancellationToken = default)
        {
            _api.Projects.GetProjects(onSuccess, onFailure, cancellationToken);
        }

        public void SearchProjects(string search,
                                   Action<ProjectsGetProjectsResponse> onSuccess,
                                   Action<ApiError> onFailure,
                                   CancellationToken cancellationToken = default)
        {
            ProjectsGetProjectsRequest parameters = new ProjectsGetProjectsRequest
            {
                FilterSearch = search,
                PerPage = 25
            };
            _api.Projects.GetProjects(parameters, onSuccess, onFailure, cancellationToken);
        }

        public Task<ApiResult<ModelVersionsGetActivitiesResponse>> LoadActivitiesAsync(
            int modelVersionId,
            CancellationToken cancellationToken = default)
        {
            return _api.ModelVersions.GetActivitiesAsync(modelVersionId, cancellationToken);
        }

        public Task<ApiResult<ProjectsGetProjectsResponse>> SearchProjectsAsync(
            string search,
            CancellationToken cancellationToken = default)
        {
            ProjectsGetProjectsRequest parameters = new ProjectsGetProjectsRequest
            {
                FilterSearch = search,
                PerPage = 25
            };
            return _api.Projects.GetProjectsAsync(parameters, cancellationToken);
        }

        public ApiRequest InspectActivitiesRequest(int modelVersionId)
        {
            ModelVersionsGetActivitiesRequest parameters = new ModelVersionsGetActivitiesRequest
            {
                VersionId = modelVersionId
            };
            return _api.CreateRequest(SimultriaEndpoints.ModelVersions.GetActivities, parameters);
        }
        #endregion
    }
}
