// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Read;

/// <summary>
/// Represents a paged result.
/// </summary>
/// <typeparam name="TItem">Type of items.</typeparam>
/// <param name="Items">The items that was fetched.</param>
/// <param name="TotalCount">The total number of items.</param>
public record PagedQueryResult<TItem>(IEnumerable<TItem> Items, ulong TotalCount);
