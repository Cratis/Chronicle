// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Applies observer tag and route filters to client-side event reads.
/// </summary>
/// <param name="FilterTags">The OR tag filters, distinct from observer labels.</param>
/// <param name="EventSourceType">The source category, or Unspecified for all sources.</param>
/// <param name="EventStreamType">The stream category, or All for all streams.</param>
internal sealed record ObserverEventFilter(
    IReadOnlyList<string> FilterTags,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType)
{
    /// <summary>
    /// Resolves filters from the observer's declaration and owning source registry.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventSources">The owning source definitions.</param>
    /// <returns>The resolved filter definition.</returns>
    internal static ObserverEventFilter For(Type observerType, IEventSources eventSources)
    {
        var (source, stream) = observerType.GetEventSourceFilter(eventSources);
        return new(observerType.GetFilterTags().ToArray(), source, stream);
    }

    /// <summary>
    /// Checks all categories, with OR within tags and AND across categories.
    /// </summary>
    /// <param name="context">The stored event metadata.</param>
    /// <returns>Whether the stored context passes the observer filters.</returns>
    internal bool Matches(EventContext context) =>
        (EventSourceType == Events.EventSourceType.Unspecified || context.EventSourceType == EventSourceType) &&
        (EventStreamType.IsAll || context.EventStreamType == EventStreamType) &&
        (FilterTags.Count == 0 || FilterTags.Any(tag => context.Tags.Any(eventTag => eventTag.Value == tag)));
}
