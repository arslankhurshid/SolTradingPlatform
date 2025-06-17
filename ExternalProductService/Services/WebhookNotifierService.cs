using ExternalProductService.Models; // Modell einbinden
using System.Net.Http;
using System.Net.Http.Json; // Für PostAsJsonAsync
using System.Threading.Tasks;

namespace ExternalProductService.Services
{
    public class WebhookNotifierService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WebhookNotifierService> _logger;
        private const string WebhookListenerUrl = "http://localhost:5299/api/webhook/newreview"; 

        public WebhookNotifierService(IHttpClientFactory httpClientFactory, ILogger<WebhookNotifierService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task NotifyNewReviewAsync(ProductReview review)
        {
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                _logger.LogInformation("Sende Webhook für neue Bewertung an: {WebhookUrl}", WebhookListenerUrl);
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(WebhookListenerUrl, review);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Webhook erfolgreich gesendet. Antwort vom Listener: {Response}", responseContent);
                }
                else
                {
                    _logger.LogError("Fehler beim Senden des Webhooks. Statuscode: {StatusCode}, Grund: {ReasonPhrase}",
                                     response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Netzwerkfehler beim Senden des Webhooks an {WebhookUrl}", WebhookListenerUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler beim Senden des Webhooks.");
            }
        }
    }
}
