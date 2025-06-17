using Microsoft.AspNetCore.Mvc;
using WebhookListenerService.Models; // Unser Modell einbinden

namespace WebhookListenerService.Controllers
{
    [Route("api/[controller]")] // Route: /api/webhook
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ILogger<WebhookController> logger)
        {
            _logger = logger;
        }

        // POST: api/webhook/newreview
        [HttpPost("newreview")]
        public IActionResult NewProductReview([FromBody] ProductReview review)
        {
            if (review == null)
            {
                _logger.LogWarning("Webhook: Leere Bewertungsdaten empfangen.");
                return BadRequest("Leere Bewertungsdaten.");
            }

            // Hier werden die Bewertungsdaten normalerweise verarbeitet:
            // - In einer Datenbank speichern
            // - Eine Benachrichtigung senden
            // - Weitere Aktionen auslösen

            _logger.LogInformation("Webhook: Neue Produktbewertung empfangen für Produkt-ID {ProductId} von {UserName}. Bewertung: {Rating}/5. Kommentar: {Comment}",
                review.ProductId, review.UserName, review.Rating, review.Comment);

            // Bestätigen Sie dem Sender, dass die Daten empfangen wurden.
            return Ok(new { message = "Bewertung erfolgreich empfangen!", reviewId = Guid.NewGuid() });
        }
    }
}