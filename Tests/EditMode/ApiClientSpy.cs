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

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastRequest = request;
            return Success<TResponse>(request?.Method ?? HttpMethod.GET);
        }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiEndpoint endpoint,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastEndpoint = endpoint;
            return Success<TResponse>(endpoint?.Method ?? HttpMethod.GET);
        }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            ApiEndpoint endpoint,
            object body,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            LastEndpoint = endpoint;
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

        private static Task<ApiResult<TResponse>> Success<TResponse>(
            HttpMethod method)
        {
            return Task.FromResult(
                ApiResult<TResponse>.Success(
                    default(TResponse),
                    method,
                    200,
                    null,
                    null));
        }
    }
}
