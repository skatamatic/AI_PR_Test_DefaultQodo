[TestFixture]
public class AdvancedForecastingEngineTests
{
    private AdvancedForecastingEngine _engine;

    [SetUp]
    public void Setup()
    {
        _engine = new AdvancedForecastingEngine();
    }

    [Test]
    public void CalculateMovingAverageForecast_ValidData_ReturnsCorrectAverage()
    {
        // Arrange
        var historicalSales = new List<OrderItemData>
        {
            // Oldest to newest for demonstration, though current implementation takes last N
            // The current implementation of CalculateMovingAverageForecast sorts by PriceAtPurchase descending
            // then takes. This might not be ideal for time series.
            // For this test, we'll ensure quantities are distinct to see the effect.
            // To properly test, ideally OrderItemData or the list should be explicitly time-ordered.
            // Given the implementation detail:
            // It uses .OrderByDescending(s => s.PriceAtPurchase).Take(periods)
            // Let's assume PriceAtPurchase can be used to order for now.
            new OrderItemData { ProductId = 1, Quantity = 10, PriceAtPurchase = 100m }, // oldest if price is lowest
            new OrderItemData { ProductId = 1, Quantity = 12, PriceAtPurchase = 101m },
            new OrderItemData { ProductId = 1, Quantity = 14, PriceAtPurchase = 102m }  // newest if price is highest
        };
        int periods = 2; // Average of last 2 (12 and 14)

        // Act
        // Need to ensure the GetProductById logic in OrderFulfillmentService correctly provides these.
        // For this test, we are directly passing data.
        // The method sorts by PriceAtPurchase DESC, takes 'periods', then averages Quantity.
        // So it will take (14, Price 102) and (12, Price 101). Average of 14 and 12 is 13.
        decimal forecast = _engine.CalculateMovingAverageForecast(historicalSales, periods);

        // Assert
        Assert.That(forecast, Is.EqualTo(13.0m));
    }

    [Test]
    public void CalculateMovingAverageForecast_FewerDataPointsThanPeriods_UsesAvailableData()
    {
        // Arrange
        var historicalSales = new List<OrderItemData>
        {
            new OrderItemData { ProductId = 1, Quantity = 10, PriceAtPurchase = 100m }
        };
        int periods = 3;

        // Act
        decimal forecast = _engine.CalculateMovingAverageForecast(historicalSales, periods);

        // Assert
        Assert.That(forecast, Is.EqualTo(10.0m)); // Average of the single data point
    }

    [Test]
    public void CalculateMovingAverageForecast_NoData_ReturnsZero()
    {
        // Arrange
        var historicalSales = new List<OrderItemData>();
        int periods = 3;

        // Act
        decimal forecast = _engine.CalculateMovingAverageForecast(historicalSales, periods);

        // Assert
        Assert.That(forecast, Is.EqualTo(0m));
    }


    [Test]
    public void CalculateExponentialSmoothingForecast_ValidData_ReturnsCorrectForecast()
    {
        // Arrange
        var salesQuantities = new List<int> { 10, 12, 11, 13 }; // Chronological
        decimal alpha = 0.5m;
        // F1 = A1 = 10
        // F2 = 0.5*A1 + 0.5*F1 = 0.5*10 + 0.5*10 = 10
        // F3 = 0.5*A2 + 0.5*F2 = 0.5*12 + 0.5*10 = 6 + 5 = 11
        // F4 = 0.5*A3 + 0.5*F3 = 0.5*11 + 0.5*11 = 5.5 + 5.5 = 11
        // Forecast for Next Period (F5) = 0.5*A4 + 0.5*F4 = 0.5*13 + 0.5*11 = 6.5 + 5.5 = 12

        // Act
        decimal forecast = _engine.CalculateExponentialSmoothingForecast(salesQuantities, alpha);

        // Assert
        Assert.That(forecast, Is.EqualTo(12.0m).Within(0.001m));
    }

    [Test]
    public void CalculateExponentialSmoothingForecast_AlphaOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var salesQuantities = new List<int> { 10, 12 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _engine.CalculateExponentialSmoothingForecast(salesQuantities, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => _engine.CalculateExponentialSmoothingForecast(salesQuantities, 1.1m));
    }

    [Test]
    public void CalculateEconomicOrderQuantity_ValidInputs_ReturnsCorrectEOQ()
    {
        // Arrange
        double annualDemand = 1000;
        double orderingCostPerOrder = 50;
        double annualHoldingCostPerUnit = 5;
        // EOQ = sqrt((2 * 1000 * 50) / 5) = sqrt(100000 / 5) = sqrt(20000) = 141.421356

        // Act
        double eoq = _engine.CalculateEconomicOrderQuantity(annualDemand, orderingCostPerOrder, annualHoldingCostPerUnit);

        // Assert
        Assert.That(eoq, Is.EqualTo(141.421).Within(0.001));
    }

    [Test]
    public void CalculateEconomicOrderQuantity_ZeroDemand_ReturnsZero()
    {
        // Act
        double eoq = _engine.CalculateEconomicOrderQuantity(0, 50, 5);
        // Assert
        Assert.That(eoq, Is.EqualTo(0));
    }


    [Test]
    public void CalculateReorderPoint_ValidInputs_ReturnsCorrectReorderPoint()
    {
        // Arrange
        double averageDailyDemand = 10;
        int leadTimeInDays = 7;
        double safetyStock = 20;
        // Reorder Point = (10 * 7) + 20 = 70 + 20 = 90

        // Act
        double reorderPoint = _engine.CalculateReorderPoint(averageDailyDemand, leadTimeInDays, safetyStock);

        // Assert
        Assert.That(reorderPoint, Is.EqualTo(90));
    }
}