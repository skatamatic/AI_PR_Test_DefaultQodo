public class InMemoryProductRepository : IProductRepository
{
    // In a real app, this would be a database context
    public static List<ProductData> AllProducts { get; set; } = new List<ProductData>
    {
        new ProductData { Id = 1, Name = "Laptop Pro", CurrentPrice = 1200.00m, Category = "Electronics", StockQuantity = 50 },
        new ProductData { Id = 2, Name = "Wireless Mouse", CurrentPrice = 25.00m, Category = "Accessories", StockQuantity = 200 },
        new ProductData { Id = 3, Name = "Mechanical Keyboard", CurrentPrice = 75.00m, Category = "Accessories", StockQuantity = 100 }
    };
    private static int _nextProductId = AllProducts.Max(p => p.Id) + 1;


    public ProductData GetProductById(int productId)
    {
        return AllProducts.FirstOrDefault(p => p.Id == productId);
    }

    public IEnumerable<ProductData> GetAllProducts()
    {
        return AllProducts;
    }

    public ProductData AddProduct(ProductData product) // Not in interface, but useful for setup
    {
        product.Id = _nextProductId++;
        AllProducts.Add(product);
        return product;
    }

    public void UpdateProductStock(int productId, int newStockLevel)
    {
        var product = GetProductById(productId);
        if (product != null)
        {
            product.StockQuantity = newStockLevel;
            Console.WriteLine($"SOLID: Stock updated for {product.Name} to {newStockLevel}.");
        }
        else
        {
            Console.WriteLine($"SOLID Error: Product ID {productId} not found for stock update.");
        }
    }
}
