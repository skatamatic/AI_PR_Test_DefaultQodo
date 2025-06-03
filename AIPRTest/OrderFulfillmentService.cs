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
            var product = _productRepo.GetProductById(itemDetail.ProductId); // Re-fetch to be safe, or use earlier instance
            int newStock = product.StockQuantity - itemDetail.Quantity;
            _productRepo.UpdateProductStock(product.Id, newStock);
            _notificationService.NotifyStockLow(product); // Check if stock is low after update
        }

        var order = new OrderData
        {
            CustomerId = customerId,
            Items = orderItemsData,
            TotalAmount = totalAmount,
            Status = "Processed" // Payment successful
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
}