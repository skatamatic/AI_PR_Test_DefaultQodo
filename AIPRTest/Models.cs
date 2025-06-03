public class ProductData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal CurrentPrice { get; set; }
    public string Category { get; set; }
    public int StockQuantity { get; set; }
}

public class OrderData
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItemData> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
}

public class OrderItemData
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

public class CreateOrderItemDetail
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}