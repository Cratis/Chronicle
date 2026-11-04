// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Handles one catalog event using the classic reducer convention.
/// </summary>
/// <typeparam name="TEvent">The handled event.</typeparam>
public class CatalogReducer<TEvent> : IReducerFor<CatalogModel<TEvent>>
{
    /// <summary>
    /// Counts an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The previous model.</param>
    /// <returns>The next model.</returns>
    public CatalogModel<TEvent> Reduce(TEvent @event, CatalogModel<TEvent>? current)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new() { Count = (current?.Count ?? 0) + 1 };
    }
}
