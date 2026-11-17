// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    /// <summary>
    /// Order events by the dimension a sort asks for, in the direction it asks for.
    /// </summary>
    /// <param name="events">The events to order.</param>
    /// <param name="order">The <see cref="EventSequenceQuerySort"/> to order by.</param>
    /// <returns>The ordered events - by sequence number for any dimension not otherwise recognized.</returns>
    static IOrderedEnumerable<AppendedEvent> Sorted(IEnumerable<AppendedEvent> events, EventSequenceQuerySort order) => order.By switch
    {
        EventSequenceQuerySortBy.Occurred => OrderBy(events, _ => _.Context.Occurred, order.Descending),
        EventSequenceQuerySortBy.EventType => OrderBy(events, _ => _.Context.EventType.Id.Value, order.Descending),
        EventSequenceQuerySortBy.EventSourceId => OrderBy(events, _ => _.Context.EventSourceId.Value, order.Descending),
        _ => OrderBy(events, _ => _.Context.SequenceNumber.Value, order.Descending)
    };

    /// <summary>
    /// Order events by a key in either direction.
    /// </summary>
    /// <typeparam name="TKey">The type of the key ordered on.</typeparam>
    /// <param name="events">The events to order.</param>
    /// <param name="key">The key to order by.</param>
    /// <param name="descending">Whether to order from the highest value down rather than from the lowest up.</param>
    /// <returns>The ordered events.</returns>
    static IOrderedEnumerable<AppendedEvent> OrderBy<TKey>(IEnumerable<AppendedEvent> events, Func<AppendedEvent, TKey> key, bool descending) =>
        descending ? events.OrderByDescending(key) : events.OrderBy(key);

    /// <summary>
    /// Narrow events to those matching a set of query criteria.
    /// </summary>
    /// <param name="events">The events to narrow.</param>
    /// <param name="criteria">The <see cref="EventSequenceQueryCriteria"/> narrowing the events.</param>
    /// <returns>Matching events - every event when the criteria narrows nothing.</returns>
    static IEnumerable<AppendedEvent> Matching(IEnumerable<AppendedEvent> events, EventSequenceQueryCriteria criteria) =>
        events.Where(_ => criteria.Matches(_.Context));

    /// <summary>
    /// Narrows the events by the supplied criteria, matching how the persistent storage providers build their queries.
    /// </summary>
    /// <remarks>
    /// Each criterion carries a sentinel meaning "do not narrow on this dimension" — an unspecified
    /// <see cref="EventSourceId"/>, a default/unspecified <see cref="EventSourceType"/>,
    /// <see cref="EventStreamType.All"/>, the default <see cref="EventStreamId"/>, and an empty event type set.
    /// Callers asking for "everything" pass those sentinels rather than <see langword="null"/>, so treating a
    /// sentinel as a value to match on would narrow every event away.
    /// </remarks>
    /// <param name="events">The events to narrow.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to narrow by, or its unspecified sentinel.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> to narrow by, or its default/unspecified sentinel.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> to narrow by, or <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> to narrow by, or its default sentinel.</param>
    /// <param name="eventTypes">The event types to narrow by, or an empty set.</param>
    /// <param name="tags">The tags to narrow by - an event matches when it carries any of them; null or an empty set means no narrowing.</param>
    /// <returns>The narrowed events.</returns>
    static IEnumerable<AppendedEvent> Filter(
        IEnumerable<AppendedEvent> events,
        EventSourceId? eventSourceId,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId,
        IEnumerable<EventType>? eventTypes,
        IEnumerable<Tag>? tags = null)
    {
        events = NarrowByEventSource(events, eventSourceId, eventSourceType);
        events = NarrowByEventStream(events, eventStreamType, eventStreamId);
        events = NarrowByEventTypes(events, eventTypes);
        return NarrowByTags(events, tags);
    }

    static IEnumerable<AppendedEvent> NarrowByEventSource(IEnumerable<AppendedEvent> events, EventSourceId? eventSourceId, EventSourceType? eventSourceType)
    {
        if (eventSourceId?.IsSpecified == true)
        {
            events = events.Where(_ => _.Context.EventSourceId == eventSourceId);
        }

        if (eventSourceType?.IsDefaultOrUnspecified == false)
        {
            events = events.Where(_ => _.Context.EventSourceType == eventSourceType);
        }

        return events;
    }

    static IEnumerable<AppendedEvent> NarrowByEventStream(IEnumerable<AppendedEvent> events, EventStreamType? eventStreamType, EventStreamId? eventStreamId)
    {
        if (eventStreamType?.IsAll == false)
        {
            events = events.Where(_ => _.Context.EventStreamType == eventStreamType);
        }

        if (eventStreamId?.IsDefault == false)
        {
            events = events.Where(_ => _.Context.EventStreamId == eventStreamId);
        }

        return events;
    }

    static IEnumerable<AppendedEvent> NarrowByEventTypes(IEnumerable<AppendedEvent> events, IEnumerable<EventType>? eventTypes)
    {
        var typeSet = eventTypes?.Select(_ => _.Id).ToHashSet();
        return typeSet?.Count > 0
            ? events.Where(_ => typeSet.Contains(_.Context.EventType.Id))
            : events;
    }

    static IEnumerable<AppendedEvent> NarrowByTags(IEnumerable<AppendedEvent> events, IEnumerable<Tag>? tags)
    {
        var tagSet = tags?.ToHashSet();
        return tagSet?.Count > 0
            ? events.Where(_ => _.Context.Tags.Any(tagSet.Contains))
            : events;
    }
}
