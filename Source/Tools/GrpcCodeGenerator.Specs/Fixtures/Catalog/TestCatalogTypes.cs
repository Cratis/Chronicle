// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649 // Multiple test fixture types intentionally grouped in one file

using System.Reactive.Subjects;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;

namespace TestAssembly.Catalog;

/// <summary>
/// Test-only attribute that mirrors BelongsToAttribute, resolved by name in TypeDiscovery.
/// </summary>
/// <param name="service">The name of the gRPC service this type belongs to.</param>
[AttributeUsage(AttributeTargets.Class)]
sealed class BelongsToAttribute(string service) : Attribute
{
    /// <summary>Gets the service name.</summary>
    public string Service => service;
}

/// <summary>
/// Strongly-typed product identifier for testing.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record ProductId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Gets the sentinel value representing an unset product identifier.</summary>
    public static readonly ProductId NotSet = new(Guid.Empty);

    /// <summary>Converts a <see cref="ProductId"/> to a <see cref="Guid"/>.</summary>
    /// <param name="id">The product identifier.</param>
    public static implicit operator Guid(ProductId id) => id.Value;

    /// <summary>Converts a <see cref="Guid"/> to a <see cref="ProductId"/>.</summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator ProductId(Guid value) => new(value);

    /// <summary>Creates a new unique product identifier.</summary>
    /// <returns>A new <see cref="ProductId"/>.</returns>
    public static ProductId New() => new(Guid.NewGuid());
}

/// <summary>
/// Strongly-typed product name for testing.
/// </summary>
/// <param name="Value">The underlying string value.</param>
public record ProductName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the sentinel value representing an unset product name.</summary>
    public static readonly ProductName NotSet = new(string.Empty);

    /// <summary>Converts a <see cref="ProductName"/> to a <see cref="string"/>.</summary>
    /// <param name="name">The product name.</param>
    public static implicit operator string(ProductName name) => name.Value;

    /// <summary>Converts a <see cref="string"/> to a <see cref="ProductName"/>.</summary>
    /// <param name="value">The string value.</param>
    public static implicit operator ProductName(string value) => new(value);
}

/// <summary>
/// Test command for registering a product.
/// </summary>
/// <param name="Id">The product identifier.</param>
/// <param name="Name">The product name.</param>
[Command]
[BelongsTo("Products")]
public record RegisterProduct(ProductId Id, ProductName Name)
{
    /// <summary>Handles the register product command.</summary>
    public void Handle() { }
}

/// <summary>
/// Test read model representing a product.
/// </summary>
/// <param name="Id">The product identifier.</param>
/// <param name="Name">The product name.</param>
[ReadModel]
[BelongsTo("Products")]
public record Product(ProductId Id, ProductName Name)
{
    /// <summary>Returns all products.</summary>
    /// <returns>An empty enumerable of products.</returns>
    public static IEnumerable<Product> GetAll() => [];

    /// <summary>Returns a product by identifier.</summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product, or null if not found.</returns>
    public static Product? GetById(ProductId id) => null;

    /// <summary>Returns an observable stream of all products.</summary>
    /// <returns>An observable subject emitting product collections.</returns>
    public static ISubject<IEnumerable<Product>> ObserveAll() =>
        new BehaviorSubject<IEnumerable<Product>>([]);
}
