// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Read model representing a product in the catalog.
/// </summary>
/// <param name="ProductId">The unique identifier of the product.</param>
/// <param name="Name">The name of the product.</param>
/// <param name="Price">The current price of the product.</param>
/// <param name="Category">The category the product belongs to.</param>
[ReadModel]
[BelongsTo("CatalogService")]
public record Product(ProductId ProductId, ProductName Name, ProductPrice Price, string Category)
{
    static readonly List<Product> _products =
    [
        new(ProductId.New(), "Laptop Pro 15", 1299.99m, "Electronics"),
        new(ProductId.New(), "Wireless Headphones", 199.99m, "Electronics"),
        new(ProductId.New(), "Coffee Maker", 89.99m, "Kitchen"),
        new(ProductId.New(), "Running Shoes", 129.99m, "Sports"),
        new(ProductId.New(), "Python Programming Book", 49.99m, "Books"),
    ];

    static readonly BehaviorSubject<IEnumerable<Product>> _allProducts = new(_products);

    /// <summary>
    /// Gets all products in the catalog.
    /// </summary>
    /// <returns>An observable sequence of all products.</returns>
    internal static ISubject<IEnumerable<Product>> AllProducts() => _allProducts;

    /// <summary>
    /// Gets a single product by its identifier.
    /// </summary>
    /// <param name="productId">The product identifier to search for.</param>
    /// <returns>The matching product or null.</returns>
    internal static async Task<Product?> GetProduct(ProductId productId)
    {
        await Task.CompletedTask;
        return _products.Find(p => p.ProductId == productId);
    }

    /// <summary>
    /// Gets all products in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A collection of products in the category.</returns>
    internal static async Task<IEnumerable<Product>> GetProductsByCategory(string category)
    {
        await Task.CompletedTask;
        return _products.Where(p => p.Category == category);
    }
}
