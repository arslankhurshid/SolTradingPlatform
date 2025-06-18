using System.Text.Json;
using FluentFTP;
using Microsoft.Extensions.Logging;
using ProductCatalogService.FTP.Models;

namespace ProductCatalogService.FTP.Services
{
    public class FtpProductService : IFtpProductService
    {
        private readonly ILogger<FtpProductService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _productsFileName;
        private readonly string _localProductsPath;
        private List<Product> _products;

        public FtpProductService(ILogger<FtpProductService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _productsFileName = "products.json";
            _localProductsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ftp_products.txt");
            _products = new List<Product>();

            _logger.LogInformation($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            _logger.LogInformation($"Local Products Path: {_localProductsPath}");

            try
            {
                LoadProductsAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing service. Using local file data.");
                LoadFromLocalFile();
            }
        }

        private void LoadFromLocalFile()
        {
            try
            {
                _logger.LogInformation($"Loading products from local file: {_localProductsPath}");
                if (File.Exists(_localProductsPath))
                {
                    _logger.LogInformation("Local file exists, reading content...");
                    var json = File.ReadAllText(_localProductsPath);
                    _logger.LogInformation($"File content length: {json.Length} characters");
                    _logger.LogInformation($"File content: {json}");
                    
                    _products = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                    _logger.LogInformation($"Successfully loaded {_products.Count} products from local file");
                }
                else
                {
                    _logger.LogWarning($"Local products file not found at path: {_localProductsPath}");
                    _logger.LogInformation("Current directory contents:");
                    var directory = Path.GetDirectoryName(_localProductsPath);
                    if (Directory.Exists(directory))
                    {
                        foreach (var file in Directory.GetFiles(directory))
                        {
                            _logger.LogInformation($"Found file: {file}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Directory does not exist: {directory}");
                    }
                    _products = new List<Product>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading products from local file: {ex.Message}");
                _products = new List<Product>();
            }
        }

        private async Task LoadProductsAsync()
        {
            var host = _configuration["FtpSettings:Host"];
            var username = _configuration["FtpSettings:Username"];
            var password = _configuration["FtpSettings:Password"];

            _logger.LogInformation($"Attempting to connect to FTP server: {host}");

            try
            {
                using var ftpClient = new FtpClient(host, username, password);
                
                _logger.LogInformation("Connecting to FTP server...");
                ftpClient.Connect();
                _logger.LogInformation("Successfully connected to FTP server");

                _logger.LogInformation($"Checking if file exists: {_productsFileName}");
                if (ftpClient.FileExists(_productsFileName))
                {
                    _logger.LogInformation("Products file found, downloading...");
                    using var stream = new MemoryStream();
                    ftpClient.DownloadStream(stream, _productsFileName);
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();
                    _products = JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
                    _logger.LogInformation($"Successfully loaded {_products.Count} products from FTP server");
                }
                else
                {
                    _logger.LogInformation("Products file not found on FTP server, using local file");
                    LoadFromLocalFile();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products from FTP server");
                _logger.LogInformation("Falling back to local file");
                LoadFromLocalFile();
            }
        }

        private async Task SaveProductsAsync(IFtpClient ftpClient)
        {
            try
            {
                _logger.LogInformation("Saving products to FTP server...");
                var json = JsonSerializer.Serialize(_products);
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                ftpClient.UploadStream(stream, _productsFileName);
                _logger.LogInformation("Successfully saved products to FTP server");

                // Also save to local file as backup
                await File.WriteAllTextAsync(_localProductsPath, json);
                _logger.LogInformation("Successfully saved products to local file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving products to FTP server");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            _logger.LogInformation($"Returning {_products.Count} products");
            return await Task.FromResult(_products);
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            _logger.LogInformation($"Looking up product with ID {id}: {(product != null ? "Found" : "Not found")}");
            return await Task.FromResult(product);
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            _logger.LogInformation($"Attempting to update product with ID {product.Id}");
            var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null)
            {
                _logger.LogWarning($"Product with ID {product.Id} not found for update");
                return false;
            }

            var index = _products.IndexOf(existingProduct);
            _products[index] = product;

            try
            {
                using var ftpClient = new FtpClient(
                    _configuration["FtpSettings:Host"],
                    _configuration["FtpSettings:Username"],
                    _configuration["FtpSettings:Password"]
                );

                ftpClient.Connect();
                await SaveProductsAsync(ftpClient);
                _logger.LogInformation($"Successfully updated product with ID {product.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product with ID {product.Id}");
                // Save to local file even if FTP fails
                try
                {
                    var json = JsonSerializer.Serialize(_products);
                    await File.WriteAllTextAsync(_localProductsPath, json);
                    _logger.LogInformation("Successfully saved products to local file after FTP failure");
                    return true;
                }
                catch (Exception localEx)
                {
                    _logger.LogError(localEx, "Error saving to local file");
                    return false;
                }
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            _logger.LogInformation($"Attempting to delete product with ID {id}");
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                _logger.LogWarning($"Product with ID {id} not found for deletion");
                return false;
            }

            _products.Remove(product);

            try
            {
                using var ftpClient = new FtpClient(
                    _configuration["FtpSettings:Host"],
                    _configuration["FtpSettings:Username"],
                    _configuration["FtpSettings:Password"]
                );

                ftpClient.Connect();
                await SaveProductsAsync(ftpClient);
                _logger.LogInformation($"Successfully deleted product with ID {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product with ID {id}");
                // Save to local file even if FTP fails
                try
                {
                    var json = JsonSerializer.Serialize(_products);
                    await File.WriteAllTextAsync(_localProductsPath, json);
                    _logger.LogInformation("Successfully saved products to local file after FTP failure");
                    return true;
                }
                catch (Exception localEx)
                {
                    _logger.LogError(localEx, "Error saving to local file");
                    return false;
                }
            }
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            _logger.LogInformation($"Attempting to add new product with ID {product.Id}");
            if (_products.Any(p => p.Id == product.Id))
            {
                _logger.LogWarning($"Product with ID {product.Id} already exists");
                return false;
            }

            _products.Add(product);

            try
            {
                using var ftpClient = new FtpClient(
                    _configuration["FtpSettings:Host"],
                    _configuration["FtpSettings:Username"],
                    _configuration["FtpSettings:Password"]
                );

                ftpClient.Connect();
                await SaveProductsAsync(ftpClient);
                _logger.LogInformation($"Successfully added new product with ID {product.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product with ID {product.Id}");
                // Save to local file even if FTP fails
                try
                {
                    var json = JsonSerializer.Serialize(_products);
                    await File.WriteAllTextAsync(_localProductsPath, json);
                    _logger.LogInformation("Successfully saved products to local file after FTP failure");
                    return true;
                }
                catch (Exception localEx)
                {
                    _logger.LogError(localEx, "Error saving to local file");
                    return false;
                }
            }
        }
    }
} 