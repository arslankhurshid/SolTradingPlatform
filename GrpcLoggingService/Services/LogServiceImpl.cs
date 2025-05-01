using Grpc.Core;
using static GrpcLoggingService.LogService; 

namespace GrpcLoggingService.Services
{
    public class LogServiceImpl : LogServiceBase
    {
        private readonly ILogger<LogServiceImpl> _logger;

        public LogServiceImpl(ILogger<LogServiceImpl> logger)
        {
            _logger = logger;
        }

        public override Task<LogResponse> LogError(LogRequest request, ServerCallContext context)
        {
            var timestamp = request.Timestamp ?? "unknown time";
            var source = request.Source ?? "unknown source";
            var message = request.Message ?? "no message";

            Console.WriteLine($"{timestamp} - {source}: {message}");

            return Task.FromResult(new LogResponse
            {
                Success = true
            });
        }
    }
}