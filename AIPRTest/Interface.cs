// Interface.cs
using System;
using System.Collections.Generic;

public interface IProductRepository
{
    ProductData GetProductById(int productId);
    IEnumerable<ProductData> GetAllProducts();
    void UpdateProductStock(int productId, int newStockLevel);
}

public interface IOrderRepository
{
    OrderData CreateOrder(OrderData order);
    OrderData GetOrderById(int orderId);
    bool UpdateOrderStatus(int orderId, string newStatus);
}

public interface IPaymentGateway
{
    bool ProcessPayment(string customerId, decimal amount);
}

public interface INotificationService
{
    void SendOrderConfirmation(string customerId, OrderData order);
    void NotifyStockLow(ProductData product);
    void SendOrderCancellationNotification(string customerId, int orderId, string reason);
}

public interface IOrderFulfillmentService
{
    OrderData PlaceOrder(string customerId, List<CreateOrderItemDetail> items);
    bool ShipOrder(int orderId);
    bool CancelOrder(int orderId, string reason);
}