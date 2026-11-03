// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Provides a distinct read model for each classic reducer in the catalog.
/// </summary>
/// <typeparam name="TEvent">The handled event.</typeparam>
public record CatalogModel<TEvent>
{
    /// <summary>
    /// Gets the number of folded events.
    /// </summary>
    public int Count { get; init; }
}
