namespace ExternalProductService.Models // Namespace anpassen!
{
    public class ProductReview
    {
        public int ProductId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}