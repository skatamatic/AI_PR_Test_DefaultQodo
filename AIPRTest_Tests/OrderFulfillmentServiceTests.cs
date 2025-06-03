using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class OrderFulfillmentServiceTests
{
    private Mock<IProductRepository> _mockProductRepo;
    private Mock<IOrderRepository> _mockOrderRepo;
    private Mock<IPaymentGateway> _mockPaymentGateway;
    private Mock<INotificationService> _mockNotificationService;
    private OrderFulfillmentService _orderService;

    [SetUp]
    public void Setup()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockPaymentGateway = new Mock<IPaymentGateway>();
        _mockNotificationService = new Mock<INotificationService>();

        _orderService = new OrderFulfillmentService(
            _mockProductRepo.Object,
            _mockOrderRepo.Object,
            _mockPaymentGateway.Object,
            _mockNotificationService.Object);
    }

    [Test]
    public void PlaceOrder_ProductNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        string customerId = "cust123";
        var itemsToOrder = new List<CreateOrderItemDetail>
            { new CreateOrderItemDetail { ProductId = 99, Quantity = 1 } };

        _mockProductRepo.Setup(repo => repo.GetProductById(99)).Returns((ProductData)null);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _orderService.PlaceOrder(customerId, itemsToOrder));
        Assert.That(ex.Message, Does.Contain("Product with ID 99 not found."));
    }

    [Test]
    public void PlaceOrder_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        string customerId = "cust123";
        var itemsToOrder = new List<CreateOrderItemDetail>
            { new CreateOrderItemDetail { ProductId = 1, Quantity = 10 } };
        var product1 = new ProductData { Id = 1, Name = "Test Product", CurrentPrice = 10.00m, StockQuantity = 5 };

        _mockProductRepo.Setup(repo => repo.GetProductById(1)).Returns(product1);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _orderService.PlaceOrder(customerId, itemsToOrder));
        Assert.That(ex.Message, Does.Contain("Not enough stock for product 'Test Product'"));
    }

    [Test]
    public void PlaceOrder_PaymentFails_ThrowsInvalidOperationException()
    {
        // Arrange
        string customerId = "cust123";
        var itemsToOrder = new List<CreateOrderItemDetail>
            { new CreateOrderItemDetail { ProductId = 1, Quantity = 2 } };
        var product1 = new ProductData { Id = 1, Name = "Test Product", CurrentPrice = 10.00m, StockQuantity = 5 };

        _mockProductRepo.Setup(repo => repo.GetProductById(1)).Returns(product1);
        _mockPaymentGateway.Setup(pg => pg.ProcessPayment(customerId, 20.00m)).Returns(false);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _orderService.PlaceOrder(customerId, itemsToOrder));
        Assert.That(ex.Message, Does.Contain("Payment processing failed."));
        _mockProductRepo.Verify(repo => repo.UpdateProductStock(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void PlaceOrder_EmptyItemsList_ThrowsArgumentException()
    {
        // Arrange
        string customerId = "cust123";
        var itemsToOrder = new List<CreateOrderItemDetail>();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _orderService.PlaceOrder(customerId, itemsToOrder));
        Assert.That(ex.Message, Does.Contain("Order must contain items."));
    }

    [Test]
    public void ShipOrder_ValidOrder_ReturnsTrueAndUpdatesStatus()
    {
        // Arrange
        int orderId = 1;
        var order = new OrderData { Id = orderId, Status = "Processed" };
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(order);
        _mockOrderRepo.Setup(repo => repo.UpdateOrderStatus(orderId, "Shipped")).Returns(true);

        // Act
        bool result = _orderService.ShipOrder(orderId);

        // Assert
        Assert.IsTrue(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(orderId, "Shipped"), Times.Once);
    }

    [Test]
    public void ShipOrder_OrderNotFound_ReturnsFalse()
    {
        // Arrange
        int orderId = 1;
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns((OrderData)null);

        // Act
        bool result = _orderService.ShipOrder(orderId);

        // Assert
        Assert.IsFalse(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ShipOrder_OrderNotProcessed_ReturnsFalse()
    {
        // Arrange
        int orderId = 1;
        var order = new OrderData { Id = orderId, Status = "Pending" };
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(order);

        // Act
        bool result = _orderService.ShipOrder(orderId);

        // Assert
        Assert.IsFalse(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CancelOrder_OrderIsProcessed_CancelsOrderUpdatesStatusRestoresStockAndNotifies()
    {
        // Arrange
        int orderId = 101;
        string customerId = "cust123";
        string reason = "Changed my mind";
        var productInOrder = new ProductData { Id = 1, Name = "Test Product", CurrentPrice = 10.00m, StockQuantity = 3 }; // Stock after PlaceOrder
        var orderToCancel = new OrderData
        {
            Id = orderId,
            CustomerId = customerId,
            Status = "Processed",
            Items = new List<OrderItemData> { new OrderItemData { ProductId = 1, Quantity = 2, PriceAtPurchase = 10.00m } }
        };

        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(orderToCancel);
        _mockProductRepo.Setup(repo => repo.GetProductById(1)).Returns(productInOrder);
        _mockOrderRepo.Setup(repo => repo.UpdateOrderStatus(orderId, "Cancelled")).Returns(true);

        // Act
        bool result = _orderService.CancelOrder(orderId, reason);

        // Assert
        Assert.IsTrue(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(orderId, "Cancelled"), Times.Once);
        _mockProductRepo.Verify(repo => repo.UpdateProductStock(1, 5), Times.Once); // 3 (current) + 2 (cancelled) = 5
        _mockNotificationService.Verify(ns => ns.SendOrderCancellationNotification(customerId, orderId, reason), Times.Once);
    }

    [Test]
    public void CancelOrder_OrderIsShipped_ReturnsFalseAndDoesNotCancel()
    {
        // Arrange
        int orderId = 102;
        string reason = "Too late";
        var shippedOrder = new OrderData { Id = orderId, Status = "Shipped" };
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(shippedOrder);

        // Act
        bool result = _orderService.CancelOrder(orderId, reason);

        // Assert
        Assert.IsFalse(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _mockProductRepo.Verify(repo => repo.UpdateProductStock(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockNotificationService.Verify(ns => ns.SendOrderCancellationNotification(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CancelOrder_OrderIsAlreadyCancelled_ReturnsFalse()
    {
        // Arrange
        int orderId = 103;
        string reason = "Trying again";
        var cancelledOrder = new OrderData { Id = orderId, Status = "Cancelled" };
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(cancelledOrder);

        // Act
        bool result = _orderService.CancelOrder(orderId, reason);

        // Assert
        Assert.IsFalse(result); // Policy: don't re-process if already cancelled
    }

    [Test]
    public void CancelOrder_OrderNotFound_ReturnsFalse()
    {
        // Arrange
        int orderId = 999;
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns((OrderData)null);

        // Act
        bool result = _orderService.CancelOrder(orderId, "Does not exist");

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void CancelOrder_NoReasonProvided_CancelsWithDefaultReasonAndRestoresStock()
    {
        // Arrange
        int orderId = 104;
        string customerId = "cust456";
        var productInOrder = new ProductData { Id = 2, Name = "Another Product", CurrentPrice = 5.00m, StockQuantity = 8 };
        var orderToCancel = new OrderData
        {
            Id = orderId,
            CustomerId = customerId,
            Status = "Processed",
            Items = new List<OrderItemData> { new OrderItemData { ProductId = 2, Quantity = 1, PriceAtPurchase = 5.00m } }
        };

        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(orderToCancel);
        _mockProductRepo.Setup(repo => repo.GetProductById(2)).Returns(productInOrder);
        _mockOrderRepo.Setup(repo => repo.UpdateOrderStatus(orderId, "Cancelled")).Returns(true);

        // Act
        bool result = _orderService.CancelOrder(orderId, " "); // Whitespace reason

        // Assert
        Assert.IsTrue(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(orderId, "Cancelled"), Times.Once);
        _mockProductRepo.Verify(repo => repo.UpdateProductStock(2, 9), Times.Once); // 8 + 1 = 9
        _mockNotificationService.Verify(ns => ns.SendOrderCancellationNotification(customerId, orderId, "No reason provided."), Times.Once);
    }

    [Test]
    public void CancelOrder_StatusNotProcessed_DoesNotReplenishStockButCancels()
    {
        // Arrange
        int orderId = 105;
        string customerId = "cust789";
        string reason = "Payment pending too long";
        var orderToCancel = new OrderData { Id = orderId, CustomerId = customerId, Status = "PendingPayment", Items = new List<OrderItemData>() }; // No items that affected stock

        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(orderToCancel);
        _mockOrderRepo.Setup(repo => repo.UpdateOrderStatus(orderId, "Cancelled")).Returns(true);

        // Act
        bool result = _orderService.CancelOrder(orderId, reason);

        // Assert
        Assert.IsTrue(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(orderId, "Cancelled"), Times.Once);
        _mockProductRepo.Verify(repo => repo.UpdateProductStock(It.IsAny<int>(), It.IsAny<int>()), Times.Never); // Stock should not be touched
        _mockNotificationService.Verify(ns => ns.SendOrderCancellationNotification(customerId, orderId, reason), Times.Once);
    }
}