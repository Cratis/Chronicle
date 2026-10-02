// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose books are added from the value of an event property.
/// </summary>
/// <param name="Id">Shelf identifier.</param>
/// <param name="Name">The name of the shelf.</param>
/// <param name="Books">The <see cref="ShelfBook"/> on the shelf.</param>
public record Shelf(Guid Id, string Name, IEnumerable<ShelfBook> Books);
