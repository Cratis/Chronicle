// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace TestAssembly.Catalog;

/// <summary>
/// A command whose response discovers a read model during mapping emission.
/// </summary>
[Command]
public record InspectNestedProduct
{
    /// <summary>
    /// Produces a response containing a read model.
    /// </summary>
    /// <returns>The nested product.</returns>
    public NestedProductResult Handle() => new(new(ProductId.NotSet, ProductName.NotSet));
}
