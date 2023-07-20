// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Defines a system that can invoke reducers.
/// </summary>
public interface IReducerInvoker
{
    /// <summary>
    /// Gets the event types the reducer is for.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Invoke the reducer for a set of events.
    /// </summary>
    /// <param name="eventsAndContexts">The events to reduce from.</param>
    /// <param name="initialReadModelContent">The initial state of the read model, can be null.</param>
    /// <returns>The reduced read model.</returns>
    /// <remarks>
    /// This is to be used for events that all have a key the same as the read model.
    /// </remarks>
    Task<object> Invoke(IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent);
}
