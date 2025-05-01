using ProductCatalogService.FTP.Models;
using System.Collections.Generic;
using System.IO;

namespace ProductCatalogService.FTP.Data
{
    public static class ProductFtpRepository
    {
        private static readonly string _txtFile = "Data/ftp_products.txt";

        public static List<Product> GetProducts()
        {
            var products = new List<Product>();

            if (!File.Exists(_txtFile))
                return products;

            var lines = File.ReadAllLines(_txtFile);
            int id = 1;

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length == 3 && decimal.TryParse(parts[1], out var price))
                {
                    products.Add(new Product
                    {
                        Id = id++,
                        Name = parts[0],
                        Price = price,
                        Description = parts[2]
                    });
                }
            }

            return products;
        }
    }
}