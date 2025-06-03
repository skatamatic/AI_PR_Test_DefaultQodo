public class EmailNotificationService : INotificationService
{
    public void SendOrderConfirmation(string customerId, OrderData order)
    {
        Console.WriteLine($"SOLID: Sending order confirmation for order {order.Id} to customer {customerId}.");
    }

    public void NotifyStockLow(ProductData product)
    {
        // Check if product is not null before accessing StockQuantity
        if (product != null && product.StockQuantity < 10) // Arbitrary low stock threshold
        {
            Console.WriteLine($"SOLID: Stock low notification for product {product.Name} (ID: {product.Id}). Current stock: {product.StockQuantity}.");
        }
    }

    public void SendOrderCancellationNotification(string customerId, int orderId, string reason)
    {
        Console.WriteLine($"SOLID: Sending order cancellation notification for order {orderId} to customer {customerId}. Reason: {reason}");
    }
}
