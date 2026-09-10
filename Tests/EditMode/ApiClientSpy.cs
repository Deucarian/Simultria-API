using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.API;
using Deucarian.API.Core;
using Deucarian.API.Models;

namespace Deucarian.Simultria.API.Tests.EditMode
{
    internal sealed class ApiClientSpy : IApiClient
    {
        internal ApiEndpoint LastEndpoint { get; private set; }

        internal ApiRequest LastRequest { get; private set; }

        internal CancellationToken LastCancellationToken { get; private set; }

        internal object ResponseData { get; set; }

        internal ApiError ResponseError { get; set; }

        internal Exception ThrownException { get; set; }

        internal int SendCount { get; private set; }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Success<TResponse>(request?.Method ?? HttpMethod.GET);
        }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiEndpoint endpoint,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastEndpoint = endpoint;
            LastCancellationToken = cancellationToken;
            return Success<TResponse>(endpoint?.Method ?? HttpMethod.GET);
        }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiEndpoint endpoint,
            object body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastEndpoint = endpoint;
            LastCancellationToken = cancellationToken;
            return Success<TResponse>(endpoint?.Method ?? HttpMethod.POST);
        }

        public Task<ApiResult<TResponse>> GetAsync<TResponse>(
            string endpoint,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task<ApiResult<TResponse>> PostAsync<TResponse>(
            string endpoint,
            object body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task<ApiResult<TResponse>> PutAsync<TResponse>(
            string endpoint,
            object body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task<ApiResult<TResponse>> PatchAsync<TResponse>(
            string endpoint,
            object body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
            string endpoint,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        private Task<ApiResult<TResponse>> Success<TResponse>(
            HttpMethod method)
        {
            SendCount++;
            if (ThrownException != null)
            {
                throw ThrownException;
            }

            if (ResponseError != null)
            {
                return Task.FromResult(ApiResult<TResponse>.Failure(ResponseError, method));
            }

            TResponse response = ResponseData is TResponse typed
                ? typed
                : default(TResponse);
            return Task.FromResult(
                ApiResult<TResponse>.Success(
                    response,
                    method,
                    200,
                    null,
                    null));
        }
    }
}
