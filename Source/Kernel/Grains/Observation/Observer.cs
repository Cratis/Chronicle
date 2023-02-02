// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a base class for all observers containing common methods and functionality.
/// </summary>
/// <typeparam name="TState">Type of state for the grain.</typeparam>
public class Observer<TState> : Grain<TState>
{
    /// <summary>
    /// Represents a filter for event types.
    /// </summary>
    /// <param name="_">The stream identity.</param>
    /// <param name="filterData">Data associated with the filter</param>
    /// <param name="item">Item to filter</param>
    /// <returns>True if its to be included, false if not.</returns>
    public static bool EventTypesFilter(IStreamIdentity _, object filterData, object item)
    {
        var appendedEvent = (item as AppendedEvent)!;
        var eventTypes = (filterData as EventType[])!;
        if (eventTypes.Length == 0)
        {
            return true;
        }
        return eventTypes.Any(_ => _.Id.Equals(appendedEvent.Metadata.Type.Id));
    }

    /// <summary>
    /// Represents a filter for event types.
    /// </summary>
    /// <param name="_">The stream identity.</param>
    /// <param name="filterData">Data associated with the filter</param>
    /// <param name="item">Item to filter</param>
    /// <returns>True if its to be included, false if not.</returns>
    public static bool EventTypesAndEventSourceIdFilter(IStreamIdentity _, object filterData, object item)
    {
        var appendedEvent = (item as AppendedEvent)!;
        var eventTypesAndEventSourceId = (filterData as EventTypesAndEventSourceId)!;

        var shouldIncludeEventType =
            eventTypesAndEventSourceId.EventTypes.Any(_ => _.Id.Equals(appendedEvent.Metadata.Type.Id)) ||
            eventTypesAndEventSourceId.EventTypes.Length == 0;

        return
            appendedEvent.Context.EventSourceId == eventTypesAndEventSourceId.EventSourceId &&
            shouldIncludeEventType;
    }

    /// <summary>
    /// Handle an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="@event">The <see cref="AppendedEvent"/> to handle.</param>
    public Task<bool> Handle(AppendedEvent @event)
    {
        return Task.FromResult(true);
    }
}
