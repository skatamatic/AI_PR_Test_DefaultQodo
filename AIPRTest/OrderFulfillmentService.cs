public class OrderFulfillmentService : IOrderFulfillmentService
{
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;

    public OrderFulfillmentService(
        IProductRepository productRepo,
        IOrderRepository orderRepo,
        IPaymentGateway paymentGateway,
        INotificationService notificationService)
    {
        _productRepo = productRepo;
        _orderRepo = orderRepo;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
    }

    public OrderData PlaceOrder(string customerId, List<CreateOrderItemDetail> items)
    {
        if (items == null || !items.Any())
            throw new ArgumentException("Order must contain items.");

        var orderItemsData = new List<OrderItemData>();
        decimal totalAmount = 0;

        foreach (var itemDetail in items)
        {
            var product = _productRepo.GetProductById(itemDetail.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product with ID {itemDetail.ProductId} not found.");
            if (product.StockQuantity < itemDetail.Quantity)
                throw new InvalidOperationException($"Not enough stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {itemDetail.Quantity}.");

            orderItemsData.Add(new OrderItemData { ProductId = itemDetail.ProductId, Quantity = itemDetail.Quantity, PriceAtPurchase = product.CurrentPrice });
            totalAmount += product.CurrentPrice * itemDetail.Quantity;
        }

        // Process payment
        if (!_paymentGateway.ProcessPayment(customerId, totalAmount))
        {
            throw new InvalidOperationException("Payment processing failed.");
        }

        // Update stock after successful payment
        foreach (var itemDetail in items)
        {
            var product = _productRepo.GetProductById(itemDetail.ProductId); // Re-fetch to ensure latest stock for update
            int newStock = product.StockQuantity - itemDetail.Quantity;
            _productRepo.UpdateProductStock(product.Id, newStock);
            // Potentially re-fetch product data for notification if stock levels are critical for it
            var updatedProduct = _productRepo.GetProductById(itemDetail.ProductId);
            _notificationService.NotifyStockLow(updatedProduct);
        }

        var order = new OrderData
        {
            CustomerId = customerId,
            Items = orderItemsData,
            TotalAmount = totalAmount,
            Status = "Processed" // Payment successful, stock deducted
        };

        var createdOrder = _orderRepo.CreateOrder(order);
        _notificationService.SendOrderConfirmation(customerId, createdOrder);
        return createdOrder;
    }

    public bool ShipOrder(int orderId)
    {
        var order = _orderRepo.GetOrderById(orderId);
        if (order == null || order.Status != "Processed")
        {
            Console.WriteLine($"SOLID Error: Order {orderId} cannot be shipped. Status: {order?.Status ?? "Not Found"}.");
            return false;
        }

        _orderRepo.UpdateOrderStatus(orderId, "Shipped");
        Console.WriteLine($"SOLID: Order {orderId} marked as Shipped.");
        // In a real system, trigger shipping logistics here
        return true;
    }

    public bool CancelOrder(int orderId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            // Decided to log and proceed rather than throw, policy can be debated.
            Console.WriteLine($"SOLID Warning: Order cancellation for ID {orderId} initiated without a reason or with whitespace reason.");
            reason = "No reason provided.";
        }

        var order = _orderRepo.GetOrderById(orderId);
        if (order == null)
        {
            Console.WriteLine($"SOLID Error: Order {orderId} not found. Cannot cancel.");
            return false;
        }

        if (order.Status == "Shipped")
        {
            Console.WriteLine($"SOLID Info: Order {orderId} is already shipped. Cannot cancel.");
            return false;
        }

        if (order.Status == "Cancelled")
        {
            Console.WriteLine($"SOLID Info: Order {orderId} is already cancelled.");
            return false; // Or true, if idempotency is desired without action
        }

        // If order was "Processed", stock would have been deducted. Replenish it.
        if (order.Status == "Processed" && order.Items != null)
        {
            Console.WriteLine($"SOLID Info: Replenishing stock for cancelled order {orderId}.");
            foreach (var item in order.Items)
            {
                var product = _productRepo.GetProductById(item.ProductId);
                if (product != null)
                {
                    int newStock = product.StockQuantity + item.Quantity;
                    _productRepo.UpdateProductStock(product.Id, newStock);
                    Console.WriteLine($"SOLID: Stock for product {product.Name} (ID: {product.Id}) updated to {newStock}.");
                }
                else
                {
                    Console.WriteLine($"SOLID Warning: Product with ID {item.ProductId} not found during stock replenishment for cancelled order {orderId}.");
                }
            }
        }
        // For other statuses like "PendingPayment", stock might not have been affected yet.

        bool statusUpdated = _orderRepo.UpdateOrderStatus(orderId, "Cancelled");
        if (statusUpdated)
        {
            Console.WriteLine($"SOLID: Order {orderId} marked as Cancelled. Reason: {reason}");
            _notificationService.SendOrderCancellationNotification(order.CustomerId, orderId, reason);
            return true;
        }
        else
        {
            Console.WriteLine($"SOLID Error: Failed to update order status to Cancelled for order {orderId}.");
            return false;
        }
    }
}