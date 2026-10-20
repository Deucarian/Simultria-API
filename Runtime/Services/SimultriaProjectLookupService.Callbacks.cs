using System;
using System.Threading;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    public sealed partial class SimultriaProjectLookupService
    {
        #region Methods
        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetProjects(Action<SimultriaCollectionResponse<SimultriaProjectDto>> onSuccess,
                                Action<ApiError> onFailure,
                                CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetProjectsAsync(cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetProject(int projectId,
                               Action<SimultriaResourceResponse<SimultriaProjectDto>> onSuccess,
                               Action<ApiError> onFailure,
                               CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetProjectAsync(projectId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetProjectModels(int projectId,
                                     Action<SimultriaCollectionResponse<SimultriaModelDto>> onSuccess,
                                     Action<ApiError> onFailure,
                                     CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetProjectModelsAsync(projectId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        #endregion
    }
}
