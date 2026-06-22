using System.Collections.Generic;

namespace NorthwindApp.Models
{
    public class ProductCategoryInfo
    {
        public short ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? CategoryName { get; set; }
    }

    public class QueryReportViewModel
    {
        public string SearchWord { get; set; } = string.Empty;
        public string CategoryFilter { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        public List<Product> MostExpensiveProducts { get; set; } = new();
        public List<Product> ProductsContainingWord { get; set; } = new();
        public List<Product> ProductsWithCategory { get; set; } = new();
        public List<Product> ProductsWithSupplier { get; set; } = new();
        public List<ProductCategoryInfo> ProductsByCategoryJoin { get; set; } = new();
        public List<Product> ProductsForSupplier { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> ProductsInOrders { get; set; } = new();
    }
}
