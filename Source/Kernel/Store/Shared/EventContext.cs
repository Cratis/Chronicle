// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Represents the context in which an event exists - typically what it was appended with.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/>.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> of the event as persisted in the event sequence.</param>
/// <param name="Occurred"><see cref="DateTimeOffset">When</see> it occurred.</param>
/// <param name="ValidFrom"><see cref="DateTimeOffset">When</see> event is considered valid from.</param>
/// <param name="TenantId">The <see cref="TenantId"/> the event was appended to.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the event.</param>
/// <param name="CausationId">The <see cref="CausationId"/> for what caused the event.</param>
/// <param name="CausedBy">Identity of what caused the event.</param>
/// <param name="ObservationState">Holds the state relevant for the observer observing.</param>
public record EventContext(
    EventSourceId EventSourceId,
    EventSequenceNumber SequenceNumber,
    DateTimeOffset Occurred,
    DateTimeOffset ValidFrom,
    TenantId TenantId,
    CorrelationId CorrelationId,
    CausationId CausationId,
    CausedBy CausedBy,
    EventObservationState ObservationState = EventObservationState.Initial)
{
    /// <summary>
    /// Creates an 'empty' <see cref="EventContext"/> with the event source id set to empty and all properties default.
    /// </summary>
    /// <returns>A new <see cref="EventContext"/>.</returns>
    public static readonly EventContext Empty = From(Guid.Empty, EventSequenceNumber.Unavailable);

    /// <summary>
    /// Creates a new <see cref="EventContext"/> from <see cref="EventSourceId"/> and other optional parameters.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create from.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event as persisted in the event sequence.</param>
    /// <param name="occurred">Optional occurred.</param>
    /// <param name="validFrom">Optional valid from.</param>
    /// <returns>A new <see cref="EventContext"/>.</returns>
    public static EventContext From(EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, DateTimeOffset? occurred = default, DateTimeOffset? validFrom = default)
    {
        return new(
            eventSourceId,
            sequenceNumber,
            occurred ?? DateTimeOffset.Now,
            validFrom ?? DateTimeOffset.MinValue,
            TenantId.Development,
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System);
    }

    /// <summary>
    /// Creates a copy of the context object with the new desired state.
    /// </summary>
    /// <param name="desiredState">The desired state.</param>
    /// <returns>A new copy with the desired state set.</returns>
    public EventContext WithState(EventObservationState desiredState) =>
        new(EventSourceId, SequenceNumber, Occurred, ValidFrom, TenantId, CorrelationId, CausationId, CausedBy, desiredState);
}
