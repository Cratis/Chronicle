// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.SharedTypeCatalog;

namespace TestAssembly.Catalog;

/// <summary>
/// Exercises command properties supplied outside the constructor.
/// </summary>
[Command]
public record RegisterProductWithOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterProductWithOptions"/> class.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    public RegisterProductWithOptions(ProductId id) => Id = id;

    /// <summary>
    /// Gets the identifier under its public property name.
    /// </summary>
    public ProductId Id { get; init; }

    /// <summary>
    /// Gets whether to include a receipt.
    /// </summary>
    public bool IncludeReceipt { get; init; }

    /// <summary>
    /// Gets or sets whether to notify.
    /// </summary>
    public bool Notify { get; set; }

    /// <summary>
    /// Gets the optional product name.
    /// </summary>
    public ProductName? Name { get; init; }

    /// <summary>
    /// Gets an optional flag.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the shared status.
    /// </summary>
    public CoreOwnedStatus Status { get; init; }

    /// <summary>
    /// Gets the optional shared value.
    /// </summary>
    public CoreOwnedValue? Details { get; init; }

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle() { }
}
