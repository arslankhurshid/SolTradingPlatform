using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using GrpcLoggingService;
using Grpc.Net.Client;

namespace CreditPaymentGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreditPaymentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LogService.LogServiceClient _logClient;
        private static int _serviceIndex = 0;
        private readonly string[] _serviceUrls = new[]
        {
            "http://localhost:6001",
            "http://localhost:6002"
        };

        public CreditPaymentController(IHttpClientFactory httpClientFactory, LogService.LogServiceClient logClient)
        {
            _httpClientFactory = httpClientFactory;
            _logClient = logClient;
        }

        [HttpPost("pay")]
        public async Task<IActionResult> MakePayment([FromBody] CreditCardPaymentRequest request)
        {
            const int maxAttemptsPerService = 2;
            int attempts = 0;

            for (int round = 0; round < _serviceUrls.Length; round++)
            {
                string currentServiceUrl = _serviceUrls[_serviceIndex];
                _serviceIndex = (_serviceIndex + 1) % _serviceUrls.Length;

                for (int retry = 0; retry < maxAttemptsPerService; retry++)
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        var response = await client.PostAsJsonAsync($"{currentServiceUrl}/CreditCard/pay", request);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            return Ok(result);
                        }

                        // Log error if response not successful
                        await LogErrorAsync($"Service responded with status code: {response.StatusCode}", currentServiceUrl);
                    }
                    catch (Exception ex)
                    {
                        await LogErrorAsync(ex.Message, currentServiceUrl);
                    }

                    await Task.Delay(200); // small wait before retry
                    attempts++;
                }
            }

            return StatusCode(500, "All payment services failed.");
        }

        private async Task LogErrorAsync(string message, string serviceUrl)
        {
            await _logClient.LogErrorAsync(new LogRequest
            {
                Source = $"Gateway → {serviceUrl}",
                Message = message,
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }
    }

    public class CreditCardPaymentRequest
    {
        public required string CardNumber { get; set; }
        public required string CardHolder { get; set; }
        public required string Expiration { get; set; }
    }
}
