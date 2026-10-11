// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelEventContext = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventContext;
using KernelObserverFilters = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Applies the kernel's source and stream observer filters to synthetic scenario input, without tag filtering.
/// </summary>
/// <param name="observerType">The observer type declaring the filters.</param>
/// <param name="eventSources">The registered event source definitions used to resolve typed filters.</param>
internal sealed class ObserverRouteFilter(Type observerType, IEventSources eventSources)
{
    readonly KernelObserverFilters _filters = CreateFilters(observerType, eventSources);

    /// <summary>
    /// Checks whether a prepared kernel event matches the observer's route filters.
    /// </summary>
    /// <param name="appendedEvent">The prepared event.</param>
    /// <returns>Whether the event's route matches.</returns>
    internal bool Matches(KernelAppendedEvent appendedEvent) => _filters.Matches(appendedEvent);

    /// <summary>
    /// Checks whether a synthetic client context matches the observer's route filters.
    /// </summary>
    /// <param name="context">The synthetic context.</param>
    /// <returns>Whether the context's route matches.</returns>
    internal bool Matches(EventContext context) => Matches(KernelAppendedEvent.Empty() with
    {
        Context = KernelEventContext.Empty with
        {
            EventSourceType = new(context.EventSourceType.Value),
            EventStreamType = new(context.EventStreamType.Value)
        }
    });

    static KernelObserverFilters CreateFilters(Type observerType, IEventSources eventSources)
    {
        var (eventSourceType, eventStreamType) = observerType.GetEventSourceFilter(eventSources);
        return new([], new(eventSourceType.Value), new(eventStreamType.Value));
    }
}
