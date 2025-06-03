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
        var order = new OrderData { Id = orderId, Status = "Pending" }; // Not "Processed"
        _mockOrderRepo.Setup(repo => repo.GetOrderById(orderId)).Returns(order);

        // Act
        bool result = _orderService.ShipOrder(orderId);

        // Assert
        Assert.IsFalse(result);
        _mockOrderRepo.Verify(repo => repo.UpdateOrderStatus(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
}
