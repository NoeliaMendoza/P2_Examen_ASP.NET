using System;
using System.Diagnostics;
using System.Linq;
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

    public async Task<IActionResult> Index()
    {
        var totalProducts = await _context.Products.CountAsync(p => p.Discontinued == 0);
        var totalCategories = await _context.Categories.CountAsync(c => c.Discontinued == 0);
        var totalSuppliers = await _context.Suppliers.CountAsync(s => s.Discontinued == 0);
        var lowStockCount = await _context.Products.CountAsync(p => p.Discontinued == 0 && p.UnitsInStock < 10);

        var featuredProducts = await _context.Products
            .Where(p => p.Discontinued == 0 && p.UnitsInStock > 0)
            .Include(p => p.Category)
            .OrderByDescending(p => p.UnitPrice)
            .Take(8)
            .ToListAsync();

        var categories = await _context.Categories
            .Where(c => c.Discontinued == 0)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalSuppliers = totalSuppliers;
        ViewBag.LowStockCount = lowStockCount;
        ViewBag.FeaturedProducts = featuredProducts;
        ViewBag.Categories = categories;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
