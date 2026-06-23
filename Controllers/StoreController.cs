using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindApp.Data;
using NorthwindApp.Models;

namespace NorthwindApp.Controllers;

public class StoreController : Controller
{
    private readonly NorthwindContext _context;

    public StoreController(NorthwindContext context)
    {
        _context = context;
    }

    // GET: Store
    public async Task<IActionResult> Index(string searchString, int page = 1)
    {
        const int pageSize = 12;
        page = Math.Max(page, 1);

        var query = _context.Products
            .Where(p => p.Discontinued == 0 && p.UnitsInStock > 0)
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

        var products = await query
            .OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["SearchString"] = searchString;

        return View(products);
    }

    // POST: Store/AddToCart/5
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult AddToCart(short productId, short quantity = 1)
    {
        var product = _context.Products
            .FirstOrDefault(p => p.ProductId == productId && p.Discontinued == 0);

        if (product == null || product.UnitsInStock < quantity)
            return Json(new { success = false, message = "Producto no disponible o stock insuficiente." });

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == productId);

        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = quantity,
                UnitPrice = product.UnitPrice ?? 0
            });
        }

        SaveCart(cart);
        return Json(new { success = true, cartCount = cart.Sum(c => c.Quantity) });
    }

    // GET: Store/Cart
    [Authorize(Roles = "Customer")]
    public IActionResult Cart()
    {
        var cart = GetCart();
        return View(cart);
    }

    // POST: Store/UpdateCart (global)
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCart(Dictionary<short, short> quantities)
    {
        var cart = GetCart();

        foreach (var kvp in quantities)
        {
            var item = cart.FirstOrDefault(c => c.ProductId == kvp.Key);
            if (item != null)
            {
                if (kvp.Value <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = kvp.Value;
            }
        }

        SaveCart(cart);
        return RedirectToAction(nameof(Cart));
    }

    // POST: Store/RemoveFromCart
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(short productId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCart(cart);
        return RedirectToAction(nameof(Cart));
    }

    // GET: Store/Checkout
    [Authorize(Roles = "Customer")]
    public IActionResult Checkout()
    {
        var cart = GetCart();
        if (cart.Count == 0)
            return RedirectToAction(nameof(Cart));

        ViewBag.Total = cart.Sum(c => c.Subtotal);
        return View(cart);
    }

    // POST: Store/Checkout
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string shipName, string shipAddress, string shipCity, string shipCountry)
    {
        var cart = GetCart();
        if (cart.Count == 0)
            return RedirectToAction(nameof(Cart));

        var email = User.Identity?.Name ?? "guest";
        var customerId = GenerateCustomerId(email);

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerId = customerId,
                CompanyName = email,
                ContactName = email
            };
            _context.Customers.Add(customer);
        }

        var maxOrderId = await _context.Orders.MaxAsync(o => (short?)o.OrderId) ?? 0;
        var orderId = (short)(maxOrderId + 1);

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = customerId,
            OrderDate = DateOnly.FromDateTime(DateTime.Now),
            RequiredDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            ShipName = shipName,
            ShipAddress = shipAddress,
            ShipCity = shipCity,
            ShipCountry = shipCountry,
            Discontinued = 0
        };

        _context.Orders.Add(order);

        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null || product.UnitsInStock < item.Quantity)
            {
                ModelState.AddModelError("", $"Stock insuficiente para {item.ProductName}.");
                ViewBag.Total = cart.Sum(c => c.Subtotal);
                return View(cart);
            }

            _context.OrderDetails.Add(new OrderDetail
            {
                OrderId = orderId,
                ProductId = item.ProductId,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Discount = 0
            });

            product.UnitsInStock = (short?)(product.UnitsInStock - item.Quantity);
            _context.Products.Update(product);
        }

        await _context.SaveChangesAsync();
        ClearCart();

        return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
    }

    // GET: Store/OrderConfirmation/5
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> OrderConfirmation(short id)
    {
        var email = User.Identity?.Name ?? "";
        var customerId = GenerateCustomerId(email);

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId);

        if (order == null)
            return NotFound();

        return View(order);
    }

    // GET: Store/MyOrders
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyOrders()
    {
        var email = User.Identity?.Name ?? "";
        var customerId = GenerateCustomerId(email);

        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId && o.Discontinued == 0)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    private static string GenerateCustomerId(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "GUEST";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(email.ToUpperInvariant()));
        return Convert.ToHexString(hash)[..5];
    }

    private string CartSessionKey => "Cart_" + (User.Identity?.Name ?? "anonymous");

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        return json == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
    }

    private void ClearCart()
    {
        HttpContext.Session.Remove(CartSessionKey);
    }
}
