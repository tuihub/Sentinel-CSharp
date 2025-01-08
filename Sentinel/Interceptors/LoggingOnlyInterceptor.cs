using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Sentinel.Interceptors
{
    public class LoggingOnlyInterceptor : Interceptor
    {
        private readonly ILogger<LoggingOnlyInterceptor> _logger;

        public LoggingOnlyInterceptor(ILogger<LoggingOnlyInterceptor> logger)
        {
            _logger = logger;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.LogInformation($"Request Headers: {context.Options.Headers}");
            _logger.LogInformation($"Request Type: {typeof(TRequest)}");
            _logger.LogInformation($"Request: {request}");

            var emptyResponse = Activator.CreateInstance<TResponse>();
            return new AsyncUnaryCall<TResponse>(
                Task.FromResult(emptyResponse),
                Task.FromResult(Metadata.Empty),
                () => Status.DefaultSuccess,
                () => Metadata.Empty,
                () => { });
        }
    }
}