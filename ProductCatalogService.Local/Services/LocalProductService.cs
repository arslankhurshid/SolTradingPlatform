using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Local.Models;

namespace ProductCatalogService.Local.Services
{
    public class LocalProductService : ILocalProductService
    {
        private readonly ILogger<LocalProductService> _logger;
        private readonly string _dataDirectory;
        private readonly string _productsFilePath;
        private List<Product> _products;

        public LocalProductService(ILogger<LocalProductService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _dataDirectory = configuration["DataStorage:BaseDirectory"] ?? "Data";
            _productsFilePath = Path.Combine(_dataDirectory, configuration["DataStorage:ProductsFileName"] ?? "products.json");
            _products = new List<Product>();
            EnsureDataDirectoryExists();
            LoadProducts();
        }

        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            if (!File.Exists(_productsFilePath))
            {
                File.WriteAllText(_productsFilePath, JsonSerializer.Serialize(new List<Product>()));
            }
        }

        private void LoadProducts()
        {
            try
            {
                var json = File.ReadAllText(_productsFilePath);
                _products = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products from file");
                _products = new List<Product>();
            }
        }

        private async Task SaveProductsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_products);
                await File.WriteAllTextAsync(_productsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving products to file");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return _products;
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                return false;
            }

            var index = _products.IndexOf(existingProduct);
            _products[index] = product;
            await SaveProductsAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            _products.Remove(product);
            await SaveProductsAsync();
            return true;
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            if (_products.Any(p => p.Id == product.Id))
            {
                return false;
            }

            _products.Add(product);
            await SaveProductsAsync();
            return true;
        }
    }
} 