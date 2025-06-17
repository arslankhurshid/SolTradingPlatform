using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using GrpcLoggingService;
using Grpc.Core;
using System.Text;
using EasyCreditPaymentService;

namespace CreditPaymentGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreditPaymentController : ControllerBase
    {
        private readonly LogService.LogServiceClient _logClient;
        private static int _serviceIndex = 0;
        private readonly string[] _serviceUrls = new[]
        {
            "http://localhost:6001", // IEGEasyCreditcardService1
            "http://localhost:6002"  // IEGEasyCreditcardService2
        };

        public CreditPaymentController(LogService.LogServiceClient logClient)
        {
            _logClient = logClient;
        }

        [HttpPost("pay")]
        public async Task<IActionResult> MakePayment([FromBody] ApiCreditCardPaymentRequest request)
        {
            var errorLog = new StringBuilder();
            const int maxAttemptsPerService = 2;

            for (int round = 0; round < _serviceUrls.Length; round++)
            {
                _serviceIndex = (_serviceIndex + 1) % _serviceUrls.Length;
                string currentServiceUrl = _serviceUrls[_serviceIndex];

                for (int retry = 0; retry < maxAttemptsPerService; retry++)
                {
                    try
                    {
                        errorLog.AppendLine($"Trying service: {currentServiceUrl}");
                        var channel = GrpcChannel.ForAddress(currentServiceUrl);
                        var grpcClient = new EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceClient(channel);

                        var grpcRequest = new CreditCardPaymentRequest
                        {
                            CardNumber = request.CardNumber,
                            CardHolder = request.CardHolder,
                            Expiry = request.Expiration
                        };
                        errorLog.AppendLine("Sending gRPC request...");
                        var grpcResponse = await grpcClient.ProcessPaymentAsync(grpcRequest);
                        errorLog.AppendLine($"Received response: Success={grpcResponse.Success}");

                        if (grpcResponse.Success)
                        {
                            return Ok(new { 
                                Message = "Payment successful",
                                TransactionId = grpcResponse.TransactionId
                            });
                        }
                        else
                        {
                            await LogErrorAsync("Payment was unsuccessful", currentServiceUrl);
                        }
                    }
                    catch (RpcException ex)
                    {
                        await LogErrorAsync($"gRPC error: {ex.StatusCode} - {ex.Message}", currentServiceUrl);
                    }
                    catch (Exception ex)
                    {
                        await LogErrorAsync($"Unexpected error: {ex.Message}", currentServiceUrl);
                    }

                    await Task.Delay(200); // delay before retry
                }
            }

            return StatusCode(500, new
            {
                Message = "All payment services failed",
                DebugInfo = errorLog.ToString()
            });
        }

        private async Task LogErrorAsync(string message, string serviceUrl)
        {
            try
            {
                var logRequest = new LogRequest
                {
                    Source = $"Gateway → {serviceUrl}",
                    Message = message,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var response = await _logClient.LogErrorAsync(logRequest);
                Console.WriteLine($"Log successful: {response.Success}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log: {ex.Message}");
            }
        }
    }

    public class ApiCreditCardPaymentRequest
    {
        public required string CardNumber { get; set; }
        public required string CardHolder { get; set; }
        public required string Expiration { get; set; }
    }
}
