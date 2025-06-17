namespace WebhookListenerService.Models
{
    public class ProductReview
    {
        public int ProductId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; } // z.B. 1-5 Sterne
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}