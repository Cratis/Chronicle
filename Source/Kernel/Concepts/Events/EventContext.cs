// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the context in which an event exists in - typically what it was appended with.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/>.</param>
/// <param name="EventStreamType">The <see cref="EventStreamType"/>.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/>.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> of the event as persisted in the event sequence.</param>
/// <param name="Occurred"><see cref="DateTimeOffset">When</see> it occurred.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> the event belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the event belongs to.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the event.</param>
/// <param name="Causation">A collection of <see cref="Causation"/> for what caused the event.</param>
/// <param name="CausedBy">A collection of Identities that caused the event.</param>
/// <param name="ObservationState">Holds the state relevant for the observer observing.</param>
public record EventContext(
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventSequenceNumber SequenceNumber,
    DateTimeOffset Occurred,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy,
    EventObservationState ObservationState = EventObservationState.Initial)
{
    /// <summary>
    /// Creates an 'empty' <see cref="EventContext"/> with the event source id set to empty and all properties default.
    /// </summary>
    /// <returns>A new <see cref="EventContext"/>.</returns>
    public static readonly EventContext Empty = From(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        Guid.Empty,
        EventStreamType.All,
        EventStreamId.Default,
        EventSequenceNumber.Unavailable,
        CorrelationId.NotSet);

    /// <summary>
    /// Creates a new <see cref="EventContext"/> from <see cref="EventSourceId"/> and other optional parameters.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the context is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the context is for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create from.</param>
    /// <param name="eventStreamType"><see cref="EventStreamType"/> to create from.</param>
    /// <param name="eventStreamId"><see cref="EventStreamId"/> to create from.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event as persisted in the event sequence.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="occurred">Optional occurred.</param>
    /// <returns>A new <see cref="EventContext"/>.</returns>
    public static EventContext From(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        DateTimeOffset? occurred = default)
    {
        return new(
            eventSourceId,
            eventStreamType,
            eventStreamId,
            sequenceNumber,
            occurred ?? DateTimeOffset.Now,
            eventStore,
            @namespace,
            correlationId,
            [],
            Identity.NotSet);
    }

    /// <summary>
    /// Creates an empty <see cref="EventContext"/> for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create for.</param>
    /// <returns>A new <see cref="EventContext"/>.</returns>
    public static EventContext EmptyWithEventSourceId(EventSourceId eventSourceId) =>
        From(
            EventStoreName.NotSet,
            EventStoreNamespaceName.NotSet,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.Unavailable,
            CorrelationId.NotSet);

    /// <summary>
    /// Creates a copy of the context object with the new desired state.
    /// </summary>
    /// <param name="desiredState">The desired state.</param>
    /// <returns>A new copy with the desired state set.</returns>
    public EventContext WithState(EventObservationState desiredState) =>
        new(
            EventSourceId,
            EventStreamType,
            EventStreamId,
            SequenceNumber,
            Occurred,
            EventStore,
            Namespace,
            CorrelationId,
            Causation,
            CausedBy,
            desiredState);
}
