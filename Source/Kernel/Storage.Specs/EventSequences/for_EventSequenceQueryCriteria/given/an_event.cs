// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria.given;

/// <summary>
/// Builds the event context the criteria matches against, so a spec only has to name the dimensions
/// it actually cares about.
/// </summary>
public static class an_event
{
    /// <summary>
    /// Build an <see cref="EventContext"/> with everything the criteria can narrow on.
    /// </summary>
    /// <param name="eventSourceId">The event source the event belongs to.</param>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="tags">The tags the event carries.</param>
    /// <param name="occurred">When the event occurred.</param>
    /// <param name="eventSourceType">The event source type the event belongs to.</param>
    /// <param name="eventStreamType">The event stream type the event belongs to.</param>
    /// <param name="correlationId">The correlation the event was appended under.</param>
    /// <returns>The <see cref="EventContext"/>.</returns>
    public static EventContext With(
        EventSourceId eventSourceId,
        EventTypeId eventType,
        IEnumerable<string>? tags = null,
        DateTimeOffset? occurred = null,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        CorrelationId? correlationId = null) =>
        EventContext.From(
            EventStoreName.NotSet,
            EventStoreNamespaceName.NotSet,
            new EventType(eventType, EventTypeGeneration.First),
            eventSourceType ?? EventSourceType.Default,
            eventSourceId,
            eventStreamType ?? EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            correlationId ?? CorrelationId.NotSet,
            tags?.Select(tag => new Tag(tag)) ?? [],
            occurred ?? DateTimeOffset.UtcNow);
}
