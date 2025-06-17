using Microsoft.AspNetCore.Mvc;
using ExternalProductService.Models; // Unser Modell
using ExternalProductService.Services; // Unseren Notifier-Service

namespace ExternalProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly WebhookNotifierService _webhookNotifier;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(WebhookNotifierService webhookNotifier, ILogger<ReviewsController> logger)
        {
            _webhookNotifier = webhookNotifier;
            _logger = logger;
        }

        // POST: api/reviews
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] ProductReview review)
        {
            if (review == null)
            {
                return BadRequest("Leere Bewertungsdaten.");
            }

            _logger.LogInformation("Neue Bewertung im ExternalProductService empfangen für Produkt-ID {ProductId}.", review.ProductId);

            // Hier könnte die Bewertung zuerst im ExternalProductService gespeichert werden.

            // Dann den Webhook senden
            await _webhookNotifier.NotifyNewReviewAsync(review);

            return Ok(new { message = "Bewertung erhalten und Webhook-Benachrichtigung initiiert." });
        }
    }
}