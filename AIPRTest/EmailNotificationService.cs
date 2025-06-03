public class EmailNotificationService : INotificationService
{
    public void SendOrderConfirmation(string customerId, OrderData order)
    {
        Console.WriteLine($"SOLID: Sending order confirmation for order {order.Id} to customer {customerId}.");
    }

    public void NotifyStockLow(ProductData product)
    {
        if (product.StockQuantity < 8) // Arbitrary low stock threshold
        {
            Console.WriteLine($"SOLID: Stock low notification for product {product.Name} (ID: {product.Id}). Current stock: {product.StockQuantity}.");
        }
    }
}
