// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestAssembly.Catalog;

/// <summary>
/// A response containing a read model not otherwise discovered by the service.
/// </summary>
/// <param name="Product">The nested read model.</param>
public record NestedProductResult(Product Product);
