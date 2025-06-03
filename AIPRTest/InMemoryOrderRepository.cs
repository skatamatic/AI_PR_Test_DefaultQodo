public class InMemoryOrderRepository : IOrderRepository
{
    public static List<OrderData> AllOrders { get; set; } = new List<OrderData>();
    private static int _nextOrderId = 1;

    public OrderData CreateOrder(OrderData order)
    {
        order.Id = _nextOrderId++;
        order.OrderDate = DateTime.UtcNow;
        AllOrders.Add(order);
        Console.WriteLine($"SOLID: Order {order.Id} created for customer {order.CustomerId}.");
        return order;
    }

    public OrderData GetOrderById(int orderId)
    {
        return AllOrders.FirstOrDefault(o => o.Id == orderId);
    }

    public bool UpdateOrderStatus(int orderId, string newStatus)
    {
        var order = GetOrderById(orderId);
        if (order != null)
        {
            order.Status = newStatus;
            Console.WriteLine($"SOLID: Order {orderId} status updated to {newStatus}.");
            return true;
        }
        return false;
    }
}
