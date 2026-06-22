using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindApp.Data;
using NorthwindApp.Models;

namespace NorthwindApp.Controllers;

public class HomeController : Controller
{
    private readonly NorthwindContext _context;

    public HomeController(NorthwindContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Queries()
    {
        const string searchWord = "Chai";
        const string categoryFilter = "Beverages";
        const string supplierName = "Exotic Liquids";

        var mostExpensiveProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.UnitPrice)
            .Take(10)
            .ToListAsync();

        var productsContainingWord = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.ProductName.Contains(searchWord) && p.UnitPrice > 10)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        var productsWithCategory = await _context.Products
            .Include(p => p.Category)
            .OrderBy(p => p.ProductName)
            .Take(10)
            .ToListAsync();

        var productsWithSupplier = await _context.Products
            .Include(p => p.Supplier)
            .OrderBy(p => p.ProductName)
            .Take(10)
            .ToListAsync();

        var productsByCategoryJoin = await _context.Products
            .Join(_context.Categories,
                p => p.CategoryId,
                c => c.CategoryId,
                (p, c) => new { Product = p, Category = c })
            .Where(pc => pc.Category.CategoryName == categoryFilter)
            .Select(pc => new ProductCategoryInfo
            {
                ProductId = pc.Product.ProductId,
                ProductName = pc.Product.ProductName,
                CategoryName = pc.Category.CategoryName
            })
            .OrderBy(pc => pc.ProductName)
            .ToListAsync();

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.CompanyName == supplierName);

        var productsForSupplier = supplier == null
            ? new List<Product>()
            : await _context.Products
                .Include(p => p.Supplier)
                .Where(p => p.SupplierId == supplier.SupplierId && p.UnitPrice > 20 && p.Discontinued == 0)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

        var recentOrders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .ToListAsync();

        var productIdsInOrders = await _context.OrderDetails
            .Select(od => od.ProductId)
            .Distinct()
            .ToListAsync();

        var productsInOrders = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => productIdsInOrders.Contains(p.ProductId))
            .OrderBy(p => p.ProductName)
            .Take(10)
            .ToListAsync();

        var viewModel = new QueryReportViewModel
        {
            SearchWord = searchWord,
            CategoryFilter = categoryFilter,
            SupplierName = supplierName,
            MostExpensiveProducts = mostExpensiveProducts,
            ProductsContainingWord = productsContainingWord,
            ProductsWithCategory = productsWithCategory,
            ProductsWithSupplier = productsWithSupplier,
            ProductsByCategoryJoin = productsByCategoryJoin,
            ProductsForSupplier = productsForSupplier,
            RecentOrders = recentOrders,
            ProductsInOrders = productsInOrders
        };

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
