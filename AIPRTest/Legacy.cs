using System.Text; // For CSV

public class LegacyAnalyticsAndReportingEngine
{
    public LegacyAnalyticsAndReportingEngine()
    {
        Console.WriteLine("LegacyAnalyticsAndReportingEngine Initialized. It might load historical data or establish direct DB connections here.");
    }

    public string GenerateComplexSalesReport(DateTime startDate, DateTime endDate, string categoryFilter = null)
    {
        Console.WriteLine($"MONOLITH: Generating complex sales report from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}. Category: {categoryFilter ?? "All"}");
        var reportBuilder = new StringBuilder();
        reportBuilder.AppendLine("--- Legacy Sales Report ---");
        reportBuilder.AppendLine($"Period: {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(categoryFilter))
        {
            reportBuilder.AppendLine($"Category Filter: {categoryFilter}");
        }

        var ordersInPeriod = InMemoryOrderRepository.AllOrders
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Shipped") // Only shipped orders count as sales
            .ToList();

        if (!ordersInPeriod.Any())
        {
            reportBuilder.AppendLine("No sales in this period.");
            return reportBuilder.ToString();
        }

        decimal totalSalesValue = 0;
        var productSales = new Dictionary<int, (int Quantity, decimal Value)>();

        foreach (var order in ordersInPeriod)
        {
            foreach (var item in order.Items)
            {
                var product = InMemoryProductRepository.AllProducts.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    if (!string.IsNullOrEmpty(categoryFilter) && product.Category != categoryFilter)
                    {
                        continue; // Skip if category doesn't match filter
                    }

                    if (!productSales.ContainsKey(item.ProductId))
                    {
                        productSales[item.ProductId] = (0, 0m);
                    }
                    productSales[item.ProductId] = (
                        productSales[item.ProductId].Quantity + item.Quantity,
                        productSales[item.ProductId].Value + (item.Quantity * item.PriceAtPurchase)
                    );
                    totalSalesValue += item.Quantity * item.PriceAtPurchase;
                }
            }
        }

        reportBuilder.AppendLine("\nProduct Sales Breakdown:");
        foreach (var entry in productSales.OrderByDescending(e => e.Value.Value))
        {
            var product = InMemoryProductRepository.AllProducts.FirstOrDefault(p => p.Id == entry.Key);
            reportBuilder.AppendLine($"  - {product?.Name ?? "Unknown Product"} (ID: {entry.Key}): {entry.Value.Quantity} units, Total Value: {entry.Value.Value:C}");
        }

        reportBuilder.AppendLine($"\nTotal Orders Processed (Shipped): {ordersInPeriod.Count}");
        reportBuilder.AppendLine($"Total Sales Value: {totalSalesValue:C}");
        reportBuilder.AppendLine("--- End of Legacy Sales Report ---");

        Console.WriteLine("MONOLITH: Complex sales report generated.");
        return reportBuilder.ToString();
    }

    public Dictionary<string, decimal> GetSalesTrendByCategory(int monthsToGoBack)
    {
        Console.WriteLine($"MONOLITH: Calculating sales trend by category for the last {monthsToGoBack} months.");
        var trends = new Dictionary<string, decimal>();
        var today = DateTime.UtcNow;

        for (int i = 0; i < monthsToGoBack; i++)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var ordersInMonth = InMemoryOrderRepository.AllOrders
                .Where(o => o.OrderDate >= monthStart && o.OrderDate <= monthEnd && o.Status == "Shipped")
                .ToList();

            foreach (var order in ordersInMonth)
            {
                foreach (var item in order.Items)
                {
                    var product = InMemoryProductRepository.AllProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        if (!trends.ContainsKey(product.Category))
                        {
                            trends[product.Category] = 0;
                        }
                        trends[product.Category] += item.Quantity * item.PriceAtPurchase;
                    }
                }
            }
        }
        Console.WriteLine("MONOLITH: Sales trend by category calculated.");
        return trends.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public string ExportFullInventoryAuditToCsv()
    {
        Console.WriteLine("MONOLITH: Exporting full inventory audit to CSV format.");
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("ProductId,ProductName,Category,CurrentPrice,StockQuantity,LastStockUpdate(Simulated)");

        // This part is monolithic because it combines product details with stock
        // and might have complex logic for 'LastStockUpdate' in a real scenario
        foreach (var product in InMemoryProductRepository.AllProducts)
        {
            // Simulate some complex logic or direct data joining
            string lastUpdateSimulated = DateTime.UtcNow.AddDays(-product.Id % 7).ToString("yyyy-MM-dd"); // Arbitrary simulation
            csvBuilder.AppendLine($"{product.Id},{EscapeCsvField(product.Name)},{EscapeCsvField(product.Category)},{product.CurrentPrice},{product.StockQuantity},{lastUpdateSimulated}");
        }
        Console.WriteLine("MONOLITH: Inventory audit CSV generated.");
        return csvBuilder.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public void PerformEndOfDayBatchProcessing()
    {
        // This could be a very complex, monolithic function that does many things:
        Console.WriteLine("\nMONOLITH: Starting End-of-Day Batch Processing...");

        // 1. Aggregate daily sales (simplified)
        var todaySales = InMemoryOrderRepository.AllOrders
            .Where(o => o.OrderDate.Date == DateTime.UtcNow.Date && o.Status == "Shipped")
            .Sum(o => o.TotalAmount);
        Console.WriteLine($"MONOLITH: Today's total sales: {todaySales:C}");

        // 2. Check for products needing reorder (directly checks stock from ProductData)
        Console.WriteLine("MONOLITH: Checking products for reorder...");
        foreach (var product in InMemoryProductRepository.AllProducts)
        {
            if (product.StockQuantity < GetReorderThreshold(product.Category))
            {
                Console.WriteLine($"MONOLITH ALERT: Product '{product.Name}' (ID: {product.Id}) stock is low ({product.StockQuantity}). Needs reordering.");
                // In a real system, this might trigger an email or create a purchase order
            }
        }

        // 3. Archive old orders (e.g., older than 1 year, very basic simulation)
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var oldOrders = InMemoryOrderRepository.AllOrders.Where(o => o.OrderDate < oneYearAgo && o.Status != "Archived").ToList();
        if (oldOrders.Any())
        {
            Console.WriteLine($"MONOLITH: Archiving {oldOrders.Count} old orders...");
            foreach (var order in oldOrders)
            {
                order.Status = "Archived"; // In real app, move to different table/storage
            }
        }

        // 4. Update some internal analytics counters (highly coupled logic)
        _internalAnalyticsCounter += InMemoryOrderRepository.AllOrders.Count(o => o.OrderDate.Date == DateTime.UtcNow.Date);
        Console.WriteLine($"MONOLITH: Internal analytics counter updated to: {_internalAnalyticsCounter}");

        Console.WriteLine("MONOLITH: End-of-Day Batch Processing Completed.\n");
    }

    private int _internalAnalyticsCounter = 0; // Example of internal state

    private int GetReorderThreshold(string category)
    {
        // Monolithic: thresholds might be hardcoded or come from a complex config this class manages
        switch (category.ToLower())
        {
            case "electronics": return 10;
            case "accessories": return 20;
            default: return 15;
        }
    }
}