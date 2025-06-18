using ProductCatalogService.Local.Models;

namespace ProductCatalogService.Local.Services
{
    public interface ILocalProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(string id);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string id);
        Task<bool> AddProductAsync(Product product);
    }
} 