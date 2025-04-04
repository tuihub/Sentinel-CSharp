using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Sentinel.Services;
using TuiHub.Protos.Librarian.Sephirah.V1;
using TuiHub.Protos.Librarian.Sephirah.V1.Sentinel;
using static TuiHub.Protos.Librarian.Sephirah.V1.Sentinel.LibrarianSentinelService;

namespace Sentinel.Interceptors
{
    public class ClientTokenInterceptor : Interceptor
    {
        private readonly ILogger<ClientTokenInterceptor> _logger;
        private readonly StateService _stateService;

        public ClientTokenInterceptor(ILogger<ClientTokenInterceptor> logger, StateService stateService)
        {
            _logger = logger;
            _stateService = stateService;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            CancellationToken ct = context.Options.CancellationToken;
            try
            {
                if (string.IsNullOrWhiteSpace(_stateService.AccessToken))
                {
                    _logger.LogWarning("No access token found, refreshing token");
                    _stateService.SetTokens(RefreshTokenAsync(_stateService.RefreshToken, ct).Result);
                }

                return ContinueWithAccessToken(request, context, continuation);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                _logger.LogInformation("Received unauthenticated error, refreshing token and retrying");

                _stateService.SetTokens(RefreshTokenAsync(_stateService.RefreshToken, ct).Result);

                return ContinueWithAccessToken(request, context, continuation);
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            throw new NotImplementedException();
        }

        private async Task<(string, string)> RefreshTokenAsync(string refreshToken, CancellationToken ct)
        {
            _logger.LogInformation("Refreshing token");
            var headers = new Metadata
            {
                { "Authorization", $"Bearer {refreshToken}" }
            };
            var client = new LibrarianSentinelServiceClient(GrpcChannel.ForAddress(_stateService.SystemConfig.LibrarianUrl));
            var token = await client.RefreshTokenAsync(new RefreshTokenRequest(), headers, cancellationToken: ct);
            ct.ThrowIfCancellationRequested();
            return (token.AccessToken, token.RefreshToken);
        }

        private AsyncUnaryCall<TResponse> ContinueWithAccessToken<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class
        {
            var metadata = context.Options.Headers ?? new Metadata();
            var authMetadata = metadata.SingleOrDefault(x => x.Key == "Authorization");
            if (authMetadata != null)
            {
                metadata.Remove(authMetadata);
            }
            metadata.Add("Authorization", $"Bearer {_stateService.AccessToken}");

            var newOptions = context.Options.WithHeaders(metadata);
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, newOptions);

            // from https://github.com/grpc/grpc-dotnet/issues/854#issuecomment-610697456
            return base.AsyncUnaryCall(request, newContext, continuation);
        }
    }
}
