using System;
using System.Net.Http;
using System.Threading.Tasks;

public class ExternalDataService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public ExternalDataService()
    {
        // Set a default timeout for HttpClient if desired
        httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GetExternalProductCategoryTrendAsync(string category)
    {
        string requestUri = $"https://jsonplaceholder.typicode.com/todos/1?category={Uri.EscapeDataString(category)}";

        try
        {
            Console.WriteLine($"EXTERNAL_DATA_SERVICE: Fetching data for category '{category}' from {requestUri}");
            HttpResponseMessage response = await httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"EXTERNAL_DATA_SERVICE: Successfully fetched data. Length: {content.Length}");
                return content;
            }
            else
            {
                Console.WriteLine($"EXTERNAL_DATA_SERVICE: Error fetching data. Status Code: {response.StatusCode}");
                return $"Error: Could not fetch data. Status: {response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"EXTERNAL_DATA_SERVICE: Request exception - {ex.Message}");
            return $"Error: Request failed - {ex.Message}";
        }
        catch (TaskCanceledException ex) // Handles timeouts
        {
            Console.WriteLine($"EXTERNAL_DATA_SERVICE: Request timed out - {ex.Message}");
            return $"Error: Request timed out - {ex.Message}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXTERNAL_DATA_SERVICE: An unexpected error occurred - {ex.Message}");
            return $"Error: An unexpected error occurred - {ex.Message}";
        }
    }

    // Example of a method that might run multiple async operations
    public async Task<Tuple<string, string>> GetMultipleExternalDataPointsAsync(string param1, string param2)
    {
        Console.WriteLine("EXTERNAL_DATA_SERVICE: Starting multiple async data fetch.");
        var task1 = GetExternalProductCategoryTrendAsync(param1); // Reusing the method for demo
        var task2 = GetExternalProductCategoryTrendAsync(param2); // Reusing the method for demo

        // Await both tasks to complete
        await Task.WhenAll(task1, task2);

        Console.WriteLine("EXTERNAL_DATA_SERVICE: Both async data fetches completed.");
        return Tuple.Create(await task1, await task2);
    }
}