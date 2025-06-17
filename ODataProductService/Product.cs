namespace ODataProductService
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }
    }


    // --- Produktdaten (hardcoded: könnten auch aus DB kommen)
    public static class FakeDatabase
    {
        public static readonly List<ODataProductService.Product> Products = new()
    {
        new ODataProductService.Product { Id = 1, Name = "Apfel", Price = 3.39 },
        new ODataProductService.Product { Id = 2, Name = "Birne", Price = 3.99 },
        new ODataProductService.Product { Id = 3, Name = "Chips", Price = 2.25 },
        new ODataProductService.Product { Id = 4, Name = "Dosenbier", Price = 1.39 },
        new ODataProductService.Product { Id = 5, Name = "Eisbergsalat", Price = 1.99 }
    };
    }
}
