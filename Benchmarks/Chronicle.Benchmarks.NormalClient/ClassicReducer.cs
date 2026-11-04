// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>A classic reducer for one event.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
public class ClassicReducer<TEvent> : IReducerFor<ClassicModel<TEvent>>
{
    /// <summary>Counts an event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The previous model.</param>
    /// <returns>The next model.</returns>
    public ClassicModel<TEvent> Reduce(TEvent @event, ClassicModel<TEvent>? current)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new() { Count = (current?.Count ?? 0) + 1 };
    }
}
