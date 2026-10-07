using System;
using System.Threading;
using Deucarian.API.Core;
using Deucarian.API.Models;
using Deucarian.Simultria.API.Models;

namespace Deucarian.Simultria.API.Services
{
    public sealed partial class SimultriaUnityBuildVersionLookupService
    {
        #region Methods
        /// <summary>Runs the lookup and invokes one typed success or failure callback on the Unity main thread.</summary>
        /// <remarks>Cancellation invokes onFailure with ApiError.IsCancellation set. Existing async behavior is shared.</remarks>
        public void GetBuildVersion(string buildVersion,
                                    string product,
                                    Action<SimultriaResourceResponse<SimultriaUnityBuildVersionDto>> onSuccess,
                                    Action<ApiError> onFailure,
                                    CancellationToken cancellationToken = default)
        {
            ApiCallbacks.Run(() => GetBuildVersionAsync(buildVersion, product, cancellationToken),
                             onSuccess,
                             onFailure,
                             cancellationToken
            );
        }

        #endregion
    }
}
