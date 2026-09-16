// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes an appended event using the metadata returned by storage, rather than client-side defaults.
/// </summary>
/// <param name="EventStore">The event store containing the event.</param>
/// <param name="Namespace">The namespace containing the event.</param>
/// <param name="SequenceNumber">The assigned sequence number.</param>
/// <param name="EventTypeId">The appended event type identifier.</param>
/// <param name="Generation">The appended event type generation.</param>
/// <param name="Tombstone">Whether the event type is a tombstone.</param>
/// <param name="EventSourceType">The persisted event source type.</param>
/// <param name="EventSourceId">The persisted event source identifier.</param>
/// <param name="EventStreamType">The persisted event stream type.</param>
/// <param name="EventStreamId">The persisted event stream identifier.</param>
/// <param name="Occurred">The persisted occurrence time.</param>
/// <param name="CorrelationId">The persisted correlation identifier.</param>
/// <param name="Subject">The subject of the event.</param>
/// <param name="Tags">The persisted tags.</param>
/// <param name="Hash">The persisted content hash.</param>
/// <param name="Causation">The persisted causation chain.</param>
/// <param name="CausedBy">The persisted responsible identity.</param>
/// <param name="ObservationState">The observation state of the appended event.</param>
public record AppendReceipt(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceNumber SequenceNumber,
    EventTypeId EventTypeId,
    EventTypeGeneration Generation,
    bool Tombstone,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    Subject Subject,
    IEnumerable<Tag> Tags,
    EventHash Hash,
    IEnumerable<AppendCausation> Causation,
    AppendIdentity CausedBy,
    EventObservationState ObservationState)
{
    /// <summary>
    /// Creates a receipt from the context of an event acknowledged by storage.
    /// </summary>
    /// <param name="context">The persisted event context.</param>
    /// <returns>The authoritative append receipt.</returns>
    internal static AppendReceipt From(EventContext context) => new(
        context.EventStore,
        context.Namespace,
        context.SequenceNumber,
        context.EventType.Id,
        context.EventType.Generation,
        context.EventType.Tombstone,
        context.EventSourceType,
        context.EventSourceId,
        context.EventStreamType,
        context.EventStreamId,
        context.Occurred,
        context.CorrelationId,
        context.Subject?.IsSet == true ? context.Subject : new Subject(context.EventSourceId.Value),
        context.Tags.ToArray(),
        context.Hash,
        context.Causation.Select(AppendCausation.From).ToArray(),
        AppendIdentity.From(context.CausedBy),
        context.ObservationState);
}
