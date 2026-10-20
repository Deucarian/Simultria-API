using System;
using System.Threading;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    public sealed partial class SimultriaActivityLookupService
    {
        #region Methods
        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetActivities(int versionId,
                                  Action<SimultriaCollectionResponse<SimultriaActivityDto>> onSuccess,
                                  Action<ApiError> onFailure,
                                  CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetActivitiesAsync(versionId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetActivities<TActivity>(int versionId,
                                             Action<SimultriaCollectionResponse<TActivity>> onSuccess,
                                             Action<ApiError> onFailure,
                                             CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetActivitiesAsync<TActivity>(versionId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetActivity(int versionId,
                                int activityId,
                                Action<SimultriaResourceResponse<SimultriaActivityDto>> onSuccess,
                                Action<ApiError> onFailure,
                                CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetActivityAsync(versionId, activityId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetActivity<TActivity>(int versionId,
                                           int activityId,
                                           Action<SimultriaResourceResponse<TActivity>> onSuccess,
                                           Action<ApiError> onFailure,
                                           CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetActivityAsync<TActivity>(versionId, activityId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        #endregion
    }
}
