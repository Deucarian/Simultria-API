using System;
using System.Threading;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    public sealed partial class SimultriaModelLookupService
    {
        #region Methods
        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetModel(int modelId,
                             Action<SimultriaResourceResponse<SimultriaModelDto>> onSuccess,
                             Action<ApiError> onFailure,
                             CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetModelAsync(modelId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetModelVersion(int versionId,
                                    Action<SimultriaResourceResponse<SimultriaModelVersionDto>> onSuccess,
                                    Action<ApiError> onFailure,
                                    CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetModelVersionAsync(versionId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetActiveModelVersion(int modelId,
                                          Action<SimultriaResourceResponse<SimultriaModelVersionDto>> onSuccess,
                                          Action<ApiError> onFailure,
                                          CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetActiveModelVersionAsync(modelId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetFrozenModelVersion(int modelId,
                                          Action<SimultriaResourceResponse<SimultriaModelVersionDto>> onSuccess,
                                          Action<ApiError> onFailure,
                                          CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetFrozenModelVersionAsync(modelId, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        #endregion
    }
}
