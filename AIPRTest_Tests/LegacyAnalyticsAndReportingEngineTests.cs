[TestFixture]
public class LegacyAnalyticsAndReportingEngineTests
{
    private LegacyAnalyticsAndReportingEngine _legacyEngine;

    [SetUp]
    public void Setup()
    {
        _legacyEngine = new LegacyAnalyticsAndReportingEngine();
        // Clear and reset static data for each test to ensure isolation
        InMemoryProductRepository.AllProducts = new List<ProductData>
        {
            new ProductData { Id = 1, Name = "Laptop Pro", CurrentPrice = 1200.00m, Category = "Electronics", StockQuantity = 50 },
            new ProductData { Id = 2, Name = "Wireless Mouse", CurrentPrice = 25.00m, Category = "Accessories", StockQuantity = 200 },
            new ProductData { Id = 3, Name = "Desk Lamp", CurrentPrice = 40.00m, Category = "Home Goods", StockQuantity = 100 }
        };
        InMemoryOrderRepository.AllOrders = new List<OrderData>();
    }

    private OrderData CreateTestOrder(int id, string customerId, DateTime orderDate, string status, List<OrderItemData> items)
    {
        var order = new OrderData
        {
            Id = id,
            CustomerId = customerId,
            OrderDate = orderDate,
            Status = status,
            Items = items,
            TotalAmount = items.Sum(i => i.PriceAtPurchase * i.Quantity)
        };
        // Manually add to the static list for the legacy engine to find
        InMemoryOrderRepository.AllOrders.Add(order);
        return order;
    }

    [Test]
    public void GenerateComplexSalesReport_NoSalesData_ReturnsNoSalesMessage()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        string report = _legacyEngine.GenerateComplexSalesReport(startDate, endDate);

        // Assert
        StringAssert.Contains("No sales in this period.", report);
    }

    [Test]
    public void GenerateComplexSalesReport_WithSalesData_GeneratesReport()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var product1 = InMemoryProductRepository.AllProducts.First(p => p.Id == 1); // Laptop Pro
        var product2 = InMemoryProductRepository.AllProducts.First(p => p.Id == 2); // Wireless Mouse

        CreateTestOrder(1, "cust1", today.AddDays(-5), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }
        });
        CreateTestOrder(2, "cust2", today.AddDays(-2), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 2, Quantity = 2, PriceAtPurchase = product2.CurrentPrice }
        });
        CreateTestOrder(3, "cust3", today.AddDays(-10), "Processed", new List<OrderItemData> // Not shipped
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }
        });


        // Act
        string report = _legacyEngine.GenerateComplexSalesReport(today.AddMonths(-1), today);

        // Assert
        StringAssert.Contains("Product Sales Breakdown:", report);
        StringAssert.Contains("Laptop Pro (ID: 1): 1 units", report);
        StringAssert.Contains("Wireless Mouse (ID: 2): 2 units", report);
        StringAssert.Contains($"Total Sales Value: {(product1.CurrentPrice * 1 + product2.CurrentPrice * 2):C}", report);
        StringAssert.DoesNotContain("Desk Lamp", report); // No sales for product 3
        StringAssert.Contains("Total Orders Processed (Shipped): 2", report);
    }

    [Test]
    public void GenerateComplexSalesReport_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var product1 = InMemoryProductRepository.AllProducts.First(p => p.Id == 1); // Electronics
        var product2 = InMemoryProductRepository.AllProducts.First(p => p.Id == 2); // Accessories

        CreateTestOrder(1, "cust1", today.AddDays(-5), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }
        });
        CreateTestOrder(2, "cust2", today.AddDays(-2), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 2, Quantity = 3, PriceAtPurchase = product2.CurrentPrice }
        });

        // Act
        string report = _legacyEngine.GenerateComplexSalesReport(today.AddMonths(-1), today, categoryFilter: "Electronics");

        // Assert
        StringAssert.Contains("Category Filter: Electronics", report);
        StringAssert.Contains("Laptop Pro (ID: 1): 1 units", report);
        StringAssert.DoesNotContain("Wireless Mouse", report); // Filtered out
        StringAssert.Contains($"Total Sales Value: {(product1.CurrentPrice * 1):C}", report);
    }


    [Test]
    public void GetSalesTrendByCategory_AggregatesSalesCorrectly()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var product1 = InMemoryProductRepository.AllProducts.First(p => p.Id == 1); // Electronics
        var product2 = InMemoryProductRepository.AllProducts.First(p => p.Id == 2); // Accessories
        var product3 = InMemoryProductRepository.AllProducts.First(p => p.Id == 3); // Home Goods

        // Month 0 (current month)
        CreateTestOrder(1, "cust1", today.AddDays(-2), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }, // 1200
            new OrderItemData { ProductId = 2, Quantity = 2, PriceAtPurchase = product2.CurrentPrice }  // 50
        });
        // Month -1 (previous month)
        CreateTestOrder(2, "cust2", today.AddMonths(-1).AddDays(5), "Shipped", new List<OrderItemData>
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }, // 1200
            new OrderItemData { ProductId = 3, Quantity = 1, PriceAtPurchase = product3.CurrentPrice }  // 40
        });
        CreateTestOrder(3, "cust3", today.AddMonths(-1).AddDays(2), "Archived", new List<OrderItemData> // Not Shipped
        {
            new OrderItemData { ProductId = 1, Quantity = 1, PriceAtPurchase = product1.CurrentPrice }
        });


        // Act
        var trends = _legacyEngine.GetSalesTrendByCategory(monthsToGoBack: 2); // Current month + 1 previous month

        // Assert
        Assert.IsTrue(trends.ContainsKey("Electronics"));
        Assert.AreEqual(1200m + 1200m, trends["Electronics"]);

        Assert.IsTrue(trends.ContainsKey("Accessories"));
        Assert.AreEqual(50m, trends["Accessories"]);

        Assert.IsTrue(trends.ContainsKey("Home Goods"));
        Assert.AreEqual(40m, trends["Home Goods"]);

        // Check order (descending by value)
        var trendList = trends.ToList();
        Assert.AreEqual("Electronics", trendList[0].Key);
    }

    [Test]
    public void ExportFullInventoryAuditToCsv_GeneratesCorrectCsvHeadersAndData()
    {
        // Act
        string csv = _legacyEngine.ExportFullInventoryAuditToCsv();

        // Assert
        StringAssert.StartsWith("ProductId,ProductName,Category,CurrentPrice,StockQuantity,LastStockUpdate(Simulated)", csv);
        StringAssert.Contains("1,Laptop Pro,Electronics,1200.00,50,", csv);
        StringAssert.Contains("2,Wireless Mouse,Accessories,25.00,200,", csv);
        StringAssert.Contains("3,\"Desk, Lamp\",Home Goods,40.00,100,", csv.Replace("Desk Lamp", "\"Desk, Lamp\"")); // Test escaping if name had comma
    }

    // PerformEndOfDayBatchProcessing is harder to unit test thoroughly without more refactoring
    // as it has multiple side effects (Console.WriteLine, modifies order statuses, internal counters).
    // A test could check some observable outcomes if possible, e.g., if products needing reorder are logged.
    // For now, we'll test a small part.
    [Test]
    public void PerformEndOfDayBatchProcessing_LowStockProduct_LogsAlert()
    {
        // Arrange
        InMemoryProductRepository.AllProducts.First(p => p.Id == 1).StockQuantity = 5; // Electronics, threshold 10

        // Redirect Console.Out to capture output
        var stringWriter = new System.IO.StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        // Act
        _legacyEngine.PerformEndOfDayBatchProcessing();

        // Restore Console.Out
        Console.SetOut(originalOut);
        string output = stringWriter.ToString();

        // Assert
        StringAssert.Contains("MONOLITH ALERT: Product 'Laptop Pro' (ID: 1) stock is low (5). Needs reordering.", output);
    }
}