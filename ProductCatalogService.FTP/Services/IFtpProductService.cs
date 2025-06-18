using ProductCatalogService.FTP.Models;

namespace ProductCatalogService.FTP.Services
{
    public interface IFtpProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(string id);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string id);
        Task<bool> AddProductAsync(Product product);
    }
} 