namespace NorthwindApp.Models;

public class CartItem
{
    public short ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public short Quantity { get; set; }
    public float UnitPrice { get; set; }
    public float Subtotal => Quantity * UnitPrice;
}
