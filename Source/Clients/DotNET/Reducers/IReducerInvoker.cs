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
    /// Invoke the reducer for an event.
    /// </summary>
    /// <param name="eventContent">Content of the event.</param>
    /// <param name="initialReadModelContent">The initial state of the read model, can be null.</param>
    /// <param name="eventContext">Context of the event.</param>
    /// <returns>The reduced read model.</returns>
    Task<object> Invoke(object eventContent, object? initialReadModelContent, EventContext eventContext);

    /// <summary>
    /// Invoke the reducer for a set of events.
    /// </summary>
    /// <param name="eventsAndContexts">The events to reduce from.</param>
    /// <param name="initialReadModelContent">The initial state of the read model, can be null.</param>
    /// <returns>The reduced read model.</returns>
    /// <remarks>
    /// This is to be used for events that all have a key the same as the read model.
    /// </remarks>
    Task<object> InvokeBulk(IEnumerable<EventAndContext> eventsAndContexts, object? initialReadModelContent);
}
