// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Observation;

using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelEventContext = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventContext;
using KernelObserverFilters = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Applies kernel observer route filters, with optional tag matching, and exposes the shared passive-read matcher.
/// </summary>
internal sealed class ObserverRouteFilter
{
    readonly ObserverEventFilter _clientFilter;
    readonly KernelObserverFilters _filters;

    /// <summary>
    /// Initializes filters using the owning source definitions.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventSources">The registered source definitions.</param>
    /// <param name="includeTagFilters">Whether to include tags; existing reactor and projection callers retain route-only matching.</param>
    internal ObserverRouteFilter(Type observerType, IEventSources eventSources, bool includeTagFilters = false)
    {
        _clientFilter = ObserverEventFilter.For(observerType, eventSources);
        _filters = new(includeTagFilters ? _clientFilter.FilterTags : [], new(_clientFilter.EventSourceType.Value), new(_clientFilter.EventStreamType.Value));
    }

    /// <summary>
    /// Checks a prepared event against the kernel's observer filters.
    /// </summary>
    /// <param name="appendedEvent">The prepared event.</param>
    /// <returns>Whether the event matches.</returns>
    internal bool Matches(KernelAppendedEvent appendedEvent) => _filters.Matches(appendedEvent);

    /// <summary>
    /// Checks a synthetic context against the kernel's observer filters.
    /// </summary>
    /// <param name="context">The synthetic context.</param>
    /// <returns>Whether the context matches.</returns>
    internal bool MatchesContext(EventContext context) => Matches(KernelAppendedEvent.Empty() with
    {
        Context = KernelEventContext.Empty with
        {
            EventSourceType = new(context.EventSourceType.Value),
            EventStreamType = new(context.EventStreamType.Value),
            Tags = context.Tags.Select(tag => new KernelConcepts::Cratis.Chronicle.Concepts.Events.Tag(tag.Value)).ToArray()
        }
    });

    /// <summary>
    /// Checks a context through the same tag and category matcher used by passive reducer reads.
    /// </summary>
    /// <param name="context">The seeded context.</param>
    /// <returns>Whether the context passes the shared client filters.</returns>
    internal bool MatchesClient(EventContext context) => _clientFilter.Matches(context);
}
