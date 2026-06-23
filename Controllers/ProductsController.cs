using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NorthwindApp.Data;
using NorthwindApp.Models;

namespace NorthwindApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly NorthwindContext _context;

        public ProductsController(NorthwindContext context)
        {
            _context = context;
        }

        // GET: Products
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            var query = _context.Products
                .Where(p => p.Discontinued == 0)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    p.Category.CategoryName.Contains(searchString) ||
                    p.Supplier.CompanyName.Contains(searchString));
            }

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var productos = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["SearchString"] = searchString;

            return View(productos);
        }

        // GET: Products/Details/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Details(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id && m.Discontinued == 0);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId");
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierId");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,SupplierId,CategoryId,QuantityPerUnit,UnitPrice,UnitsInStock,UnitsOnOrder,ReorderLevel,Discontinued")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierId", product.SupplierId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.Discontinued == 0);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierId", product.SupplierId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(short id, [Bind("ProductId,ProductName,SupplierId,CategoryId,QuantityPerUnit,UnitPrice,UnitsInStock,UnitsOnOrder,ReorderLevel,Discontinued")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierId", product.SupplierId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(short? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id && m.Discontinued == 0);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(short id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.Discontinued == 0);
            if (product != null)
            {
                product.Discontinued = 1;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/ManageStock
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageStock(string searchString, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            var query = _context.Products
                .Where(p => p.Discontinued == 0)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.ProductName.Contains(searchString));
            }

            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            ViewData["SearchString"] = searchString;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;

            var products = await query
                .OrderBy(p => p.ProductName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(products);
        }

        // POST: Products/IncrementStock
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncrementStock(short productId, short quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound();

            product.UnitsInStock = (short?)(product.UnitsInStock + quantity);
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            TempData["StockMessage"] = $"Se agregaron {quantity} unidades a {product.ProductName}. Stock actual: {product.UnitsInStock}";
            return RedirectToAction(nameof(ManageStock), new { page = 1 });
        }

        // POST: Products/DecrementStock
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecrementStock(short productId, short quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound();

            if (product.UnitsInStock < quantity)
            {
                TempData["StockError"] = $"Stock insuficiente. Stock actual: {product.UnitsInStock}";
                return RedirectToAction(nameof(ManageStock));
            }

            product.UnitsInStock = (short?)(product.UnitsInStock - quantity);
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            TempData["StockMessage"] = $"Se redujeron {quantity} unidades de {product.ProductName}. Stock actual: {product.UnitsInStock}";
            return RedirectToAction(nameof(ManageStock));
        }

        // GET: Products/LowStock
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LowStock()
        {
            var products = await _context.Products
                .Where(p => p.Discontinued == 0 && p.UnitsInStock <= p.ReorderLevel)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .OrderBy(p => p.UnitsInStock)
                .ToListAsync();
            return View(products);
        }

        private bool ProductExists(short id)
        {
            return _context.Products.Any(e => e.ProductId == id && e.Discontinued == 0);
        }
    }
}
