using System;
using System.Collections.Generic;
using System.Linq;

public class AdvancedForecastingEngine
{
    public AdvancedForecastingEngine()
    {
        Console.WriteLine("AdvancedForecastingEngine Initialized.");
    }

    /// <summary>
    /// Calculates a simple moving average forecast.
    /// </summary>
    /// <param name="historicalSales">List of OrderItemData for a specific product, ordered by date.</param>
    /// <param name="periods">Number of past periods to average for the forecast.</param>
    /// <returns>Forecasted sales quantity for the next period.</returns>
    public decimal CalculateMovingAverageForecast(List<OrderItemData> historicalSales, int periods)
    {
        if (historicalSales == null || !historicalSales.Any() || periods <= 0)
        {
            Console.WriteLine("FORECAST_ENGINE: Insufficient data or invalid periods for Moving Average forecast.");
            return 0; // Or throw ArgumentException
        }

        // Get the sales quantities from the most recent 'periods'
        var recentSales = historicalSales
            .OrderByDescending(s => s.PriceAtPurchase) // Assuming PriceAtPurchase relates to time if OrderDate isn't in OrderItemData
                                                       // Ideally, OrderItemData would have an OrderDate or be linked to an OrderData with a date
            .Take(periods)
            .Select(s => (decimal)s.Quantity) // Cast quantity to decimal for average calculation
            .ToList();

        if (recentSales.Count < periods)
        {
            Console.WriteLine($"FORECAST_ENGINE: Not enough historical periods ({recentSales.Count}) for the requested window ({periods}). Using available data.");
            if (!recentSales.Any()) return 0; // No data to average
        }

        decimal forecast = recentSales.Any() ? recentSales.Average() : 0;
        Console.WriteLine($"FORECAST_ENGINE: Moving Average Forecast for next period (based on last {recentSales.Count} periods): {forecast:F2} units.");
        return forecast;
    }

    /// <summary>
    /// Calculates a simplified Exponential Smoothing forecast.
    /// </summary>
    /// <param name="historicalSalesQuantities">List of sales quantities, ordered chronologically.</param>
    /// <param name="alpha">Smoothing factor (0 < alpha <= 1).</param>
    /// <returns>Forecasted sales quantity for the next period.</returns>
    public decimal CalculateExponentialSmoothingForecast(List<int> historicalSalesQuantities, decimal alpha)
    {
        if (historicalSalesQuantities == null || !historicalSalesQuantities.Any())
        {
            Console.WriteLine("FORECAST_ENGINE: Insufficient data for Exponential Smoothing forecast.");
            return 0; // Or throw ArgumentException
        }
        if (alpha <= 0 || alpha > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 (exclusive) and 1 (inclusive).");
        }

        decimal forecast = historicalSalesQuantities[0]; // Initial forecast is the first actual value

        for (int i = 1; i < historicalSalesQuantities.Count; i++)
        {
            forecast = alpha * historicalSalesQuantities[i - 1] + (1 - alpha) * forecast;
        }
        // The forecast for the next period is based on the last actual and last forecast
        forecast = alpha * historicalSalesQuantities.Last() + (1 - alpha) * forecast;

        Console.WriteLine($"FORECAST_ENGINE: Exponential Smoothing Forecast (alpha={alpha:F2}) for next period: {forecast:F2} units.");
        return forecast;
    }


    /// <summary>
    /// Simplified Economic Order Quantity (EOQ) calculation.
    /// Assumes demand is relatively constant and known (e.g., from a forecast).
    /// </summary>
    /// <param name="annualDemand">Total annual demand for the product (units).</param>
    /// <param name="orderingCostPerOrder">Fixed cost incurred per order placed.</param>
    /// <param name="annualHoldingCostPerUnit">Cost to hold one unit in inventory for a year.</param>
    /// <returns>Optimal order quantity (EOQ).</returns>
    public double CalculateEconomicOrderQuantity(double annualDemand, double orderingCostPerOrder, double annualHoldingCostPerUnit)
    {
        if (annualDemand <= 0 || orderingCostPerOrder < 0 || annualHoldingCostPerUnit <= 0)
        {
            Console.WriteLine("FORECAST_ENGINE: Invalid inputs for EOQ calculation (demand/holding cost must be > 0, ordering cost >= 0).");
            // Returning 0 or a small number might be problematic; indicates reorder not sensible or possible with inputs
            return 0; // Or throw ArgumentException
        }

        // EOQ formula: sqrt((2 * AnnualDemand * OrderingCost) / HoldingCostPerUnit)
        double eoq = Math.Sqrt((2 * annualDemand * orderingCostPerOrder) / annualHoldingCostPerUnit);
        Console.WriteLine($"FORECAST_ENGINE: EOQ calculated: {eoq:F2} units (Annual Demand: {annualDemand}, Order Cost: {orderingCostPerOrder:C}, Holding Cost/Unit: {annualHoldingCostPerUnit:C}).");
        return eoq;
    }

    /// <summary>
    /// Determines a reorder point based on lead time demand and safety stock.
    /// </summary>
    /// <param name="averageDailyDemand">Average daily demand for the product.</param>
    /// <param name="leadTimeInDays">Lead time for receiving an order in days.</param>
    /// <param name="safetyStock">Buffer stock to prevent stockouts.</param>
    /// <returns>Reorder point quantity.</returns>
    public double CalculateReorderPoint(double averageDailyDemand, int leadTimeInDays, double safetyStock)
    {
        if (averageDailyDemand < 0 || leadTimeInDays < 0 || safetyStock < 0)
        {
            Console.WriteLine("FORECAST_ENGINE: Invalid inputs for Reorder Point calculation (all inputs must be >= 0).");
            return 0; // Or throw ArgumentException
        }

        double leadTimeDemand = averageDailyDemand * leadTimeInDays;
        double reorderPoint = leadTimeDemand + safetyStock;

        Console.WriteLine($"FORECAST_ENGINE: Reorder Point calculated: {reorderPoint:F2} units (Avg Daily Demand: {averageDailyDemand:F2}, Lead Time: {leadTimeInDays} days, Safety Stock: {safetyStock:F2}).");
        return reorderPoint;
    }
}