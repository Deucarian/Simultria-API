using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API
{
    public sealed partial class SimultriaApi
    {
        #region Methods
        private void ThrowIfLifetimeEnded()
        {
            foreach (CancellationToken lifetime in _lifetimes)
            {
                lifetime.ThrowIfCancellationRequested();
            }
        }

        private CancellationTokenSource LinkLifetimes(CancellationToken cancellationToken, out CancellationToken token)
        {
            token = cancellationToken;
            if (_lifetimes.Length == 0) return null;
            CancellationToken[] tokens = new CancellationToken[_lifetimes.Length + 1];
            Array.Copy(_lifetimes, tokens, _lifetimes.Length);
            tokens[tokens.Length - 1] = cancellationToken;
            CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(tokens);
            token = linked.Token;
            return linked;
        }

        private async Task<ApiResult<TResponse>> ExecuteAsync<TResponse>(ApiRequest request,
                                                                        CancellationToken cancellationToken)
        {
            using (CancellationTokenSource lifetime = LinkLifetimes(cancellationToken, out CancellationToken token))
            {
                token.ThrowIfCancellationRequested();
                Task<ApiResult<TResponse>> pending = _client.SendAsync<TResponse>(request, token);
                if (pending == null) throw new InvalidOperationException("The API client returned no request task.");
                if (!token.CanBeCanceled) return await pending;

                TaskCompletionSource<bool> canceled = new TaskCompletionSource<bool>();
                using (CancellationTokenRegistration registration = token.Register(() => canceled.TrySetResult(true)))
                {
                    await Task.WhenAny(pending, canceled.Task);
                    if (token.IsCancellationRequested)
                    {
                        // Async callers also finish when an owner ends, even if their transport ignores cancellation.
                        _ = ObserveCompletionAsync(pending);
                        return ApiResult<TResponse>.Failure(CancellationError(), request.Method);
                    }
                    return await pending;
                }
            }
        }

        private static ApiError CancellationError() => new ApiError
        {
            Message = "The API request was canceled because its operation or owner lifetime ended.",
            IsCancellation = true
        };

        private static async Task ObserveCompletionAsync<TResponse>(Task<ApiResult<TResponse>> pending)
        {
            try
            {
                await pending;
            }
            catch (Exception)
            {
                // The canceled operation already completed. Observe the late exception without another completion.
            }
        }
        #endregion
    }
}
