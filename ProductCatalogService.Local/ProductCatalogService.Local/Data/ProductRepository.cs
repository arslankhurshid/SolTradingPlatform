using ProductCatalogService.Local.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ProductCatalogService.Local.Data
{
    public static class ProductRepository
    {
        private static readonly string _jsonFile = "Data/products.json";

        public static List<Product> GetProducts()
        {
            if (!File.Exists(_jsonFile)) return new List<Product>();

            string json = File.ReadAllText(_jsonFile);
            return JsonSerializer.Deserialize<List<Product>>(json) ?? new List<Product>();
        }
    }
}