public class Program
{
    public static async Task Main(string[] args) // Modified for async
    {
        // --- Setup SOLID services ---
        IProductRepository productRepo = new InMemoryProductRepository();
        IOrderRepository orderRepo = new InMemoryOrderRepository();

        IPaymentGateway paymentGateway = new DummyPaymentGateway();
        INotificationService notificationService = new EmailNotificationService();

        // Ensure products exist - IDs 1, 2, 3 are used by InMemoryProductRepository by default
        // This ensures 'Laptop Pro' (ID 1) and 'Wireless Mouse' (ID 2) are definitely there.
        // The InMemoryProductRepository already initializes with 3 products.
        // Adding custom ones if empty:
        if (!productRepo.GetAllProducts().Any())
        {
            ((InMemoryProductRepository)productRepo).AddProduct(new ProductData { Id = 4, Name = "Gaming PC", CurrentPrice = 1500.00m, Category = "Electronics", StockQuantity = 20 });
            ((InMemoryProductRepository)productRepo).AddProduct(new ProductData { Id = 5, Name = "Office Chair", CurrentPrice = 150.00m, Category = "Furniture", StockQuantity = 30 });
        }


        IOrderFulfillmentService orderService = new OrderFulfillmentService(productRepo, orderRepo, paymentGateway, notificationService);

        Console.WriteLine("--- SOLID System Operations ---");
        try
        {
            var orderDetails1 = new List<CreateOrderItemDetail>
            {
                new CreateOrderItemDetail { ProductId = 1, Quantity = 1 }, // Laptop Pro
                new CreateOrderItemDetail { ProductId = 2, Quantity = 2 }  // Wireless Mouse
            };
            OrderData newOrder1 = orderService.PlaceOrder("customer123", orderDetails1);
            orderService.ShipOrder(newOrder1.Id);

            // Add more orders for Product ID 1 for forecasting data
            var orderDetails2 = new List<CreateOrderItemDetail> { new CreateOrderItemDetail { ProductId = 1, Quantity = 3 } };
            OrderData newOrder2 = orderService.PlaceOrder("customer456", orderDetails2);
            // Let's assume this order is also processed and shipped for sales data
            ((InMemoryOrderRepository)orderRepo).UpdateOrderStatus(newOrder2.Id, "Shipped");


            var orderDetails3 = new List<CreateOrderItemDetail> { new CreateOrderItemDetail { ProductId = 1, Quantity = 2 } };
            OrderData newOrder3 = orderService.PlaceOrder("customer789", orderDetails3);
            ((InMemoryOrderRepository)orderRepo).UpdateOrderStatus(newOrder3.Id, "Shipped");

            var orderDetails4 = new List<CreateOrderItemDetail> { new CreateOrderItemDetail { ProductId = 1, Quantity = 4 } }; // More data
            OrderData newOrder4 = orderService.PlaceOrder("customer101", orderDetails4);
            ((InMemoryOrderRepository)orderRepo).UpdateOrderStatus(newOrder4.Id, "Shipped");


        }
        catch (Exception ex)
        {
            Console.WriteLine($"SOLID Error: {ex.Message}");
        }
        Console.WriteLine("---------------------------------\n");


        // --- Monolithic Engine Operations ---
        Console.WriteLine("--- Monolithic Engine Operations ---");
        LegacyAnalyticsAndReportingEngine legacyEngine = new LegacyAnalyticsAndReportingEngine();

        legacyEngine.GenerateComplexSalesReport(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        legacyEngine.GetSalesTrendByCategory(3);

        Console.WriteLine("\nMonolith Inventory Audit CSV:");
        Console.WriteLine(legacyEngine.ExportFullInventoryAuditToCsv());

        legacyEngine.PerformEndOfDayBatchProcessing();
        Console.WriteLine("---------------------------------\n");

        // --- CrossFunctionalHelper Operations ---
        Console.WriteLine("--- CrossFunctionalHelper Operations ---");
        string tangledOutput = CrossFunctionalHelper.PerformComplexFormattingAndValidation("SometHinG very_important to process", 30, true);
        Console.WriteLine($"Tangled Helper Output: {tangledOutput}");
        decimal discount = CrossFunctionalHelper.CalculateSpecialDiscount(1200m, "VIP");
        Console.WriteLine($"Calculated Discount: {discount:C}");
        Console.WriteLine($"Formatted Timestamp from Helper: {CrossFunctionalHelper.GetFormattedTimestamp()}");
        Console.WriteLine("---------------------------------\n");

        // --- ExternalDataService Operations ---
        Console.WriteLine("--- ExternalDataService Operations ---");
        ExternalDataService externalService = new ExternalDataService();
        Console.WriteLine("Fetching single external data point...");
        string externalData = await externalService.GetExternalProductCategoryTrendAsync("Electronics");
        Console.WriteLine($"External Data Received: {(externalData.Length > 100 ? externalData.Substring(0, 100) + "..." : externalData)}");

        Console.WriteLine("\nFetching multiple external data points...");
        var multipleDataPoints = await externalService.GetMultipleExternalDataPointsAsync("Books", "SoftwareComponents");
        Console.WriteLine($"External Data 1 (Books): {(multipleDataPoints.Item1.Length > 70 ? multipleDataPoints.Item1.Substring(0, 70) + "..." : multipleDataPoints.Item1)}");
        Console.WriteLine($"External Data 2 (Software): {(multipleDataPoints.Item2.Length > 70 ? multipleDataPoints.Item2.Substring(0, 70) + "..." : multipleDataPoints.Item2)}");
        Console.WriteLine("---------------------------------\n");

        // --- AdvancedForecastingEngine Operations ---
        Console.WriteLine("--- AdvancedForecastingEngine Operations ---");
        AdvancedForecastingEngine forecastEngine = new AdvancedForecastingEngine();

        // Prepare data for forecasting Product ID 1 (Laptop Pro)
        // Ensure orders are somewhat spread for date simulation if OrderItemData used PriceAtPurchase for sorting.
        // For more accurate historical sales, you'd typically use OrderData.OrderDate
        List<OrderItemData> product1SalesHistory = InMemoryOrderRepository.AllOrders
            .Where(o => o.Status == "Shipped") // Consider only shipped orders as sales
            .SelectMany(o => o.Items.Where(item => item.ProductId == 1))
            // Simulating order by adding a slight variance to PriceAtPurchase if many items are added with same price
            // Or, if order of addition to AllOrders is chronological, that's fine for this demo.
            .ToList();

        // For Exponential Smoothing, we typically need a list of quantities in chronological order.
        List<int> product1SalesQuantities = product1SalesHistory.Select(s => s.Quantity).ToList();

        if (product1SalesHistory.Any())
        {
            forecastEngine.CalculateMovingAverageForecast(product1SalesHistory, periods: 3);
        }
        else
        {
            Console.WriteLine("No sales history for Product ID 1 to calculate Moving Average Forecast.");
        }

        if (product1SalesQuantities.Count >= 2) // Exponential smoothing needs at least one prior period
        {
            forecastEngine.CalculateExponentialSmoothingForecast(product1SalesQuantities, alpha: 0.3m);
        }
        else
        {
            Console.WriteLine("Not enough sales quantity history for Product ID 1 for Exponential Smoothing.");
        }


        double annualDemand = product1SalesQuantities.Sum() * (365.0 / (InMemoryOrderRepository.AllOrders.Select(o => o.OrderDate.Date).Distinct().Count() * 1.0)); // Rough annualization
        if (annualDemand == 0 && product1SalesQuantities.Any()) annualDemand = product1SalesQuantities.Sum() * 12; // Fallback if too few days
        if (annualDemand == 0) annualDemand = 50; // Default if no sales

        forecastEngine.CalculateEconomicOrderQuantity(annualDemand: annualDemand, orderingCostPerOrder: 50, annualHoldingCostPerUnit: 15); // Example values
        forecastEngine.CalculateReorderPoint(averageDailyDemand: annualDemand / 365.0, leadTimeInDays: 14, safetyStock: 10); // Example values
        Console.WriteLine("---------------------------------\n");

        Console.WriteLine("--- Complexity Graph Production ---");
        string complexityGraph = ProduceComplexityGraph("Sample Data for Complexity Graph");
        Console.WriteLine($"Produced Complexity Graph: {complexityGraph}");
        Console.WriteLine("---------------------------------\n");

        Console.WriteLine("All operations complete. Press any key to exit.");
        Console.ReadKey();
    }

    public static string ProduceComplexityGraph(string data)
    {
        var engine = new AdvancedForecastingEngine();
        var graph = CrossFunctionalHelper.PerformComplexFormattingAndValidation(data, 10, true);
        return engine.CalculateExponentialSmoothingForecast(graph, 0.1);
    }
}