// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="EventContext"/> and its contract and storage representations.
/// </summary>
internal static class EventContextConverters
{
    /// <summary>
    /// Converts an <see cref="EventContext"/> to a contract <see cref="Contracts.Sequences.EventContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Sequences.EventContext"/>.</returns>
    public static Contracts.Sequences.EventContext ToContract(this EventContext context) => new()
    {
        EventType = context.EventType.ToContract(),
        EventSourceType = context.EventSourceType,
        EventSourceId = context.EventSourceId,
        SequenceNumber = context.SequenceNumber,
        EventStreamType = context.EventStreamType,
        EventStreamId = context.EventStreamId,
        Occurred = context.Occurred,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.ToContract(),
        CausedBy = context.CausedBy.ToContract(),
        Tags = context.Tags.ToList(),
        Hash = context.Hash,
        ObservationState = context.ObservationState.ToContract()
    };

    /// <summary>
    /// Converts a storage <see cref="Concepts.Events.EventContext"/> to an <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context">The storage <see cref="Concepts.Events.EventContext"/> to convert.</param>
    /// <returns>The converted <see cref="EventContext"/>.</returns>
    public static EventContext ToApi(this Concepts.Events.EventContext context) => new(
        context.EventType.ToApi(),
        context.EventSourceType,
        context.EventSourceId,
        context.SequenceNumber,
        context.EventStreamType,
        context.EventStreamId,
        context.Occurred,
        context.CorrelationId,
        context.Causation.ToApi(),
        context.CausedBy.ToApi(),
        context.Tags.Select(tag => tag.Value),
        context.Hash,
        context.ObservationState);

    static Contracts.Events.EventObservationState ToContract(this Concepts.Events.EventObservationState state) => state switch
    {
        Concepts.Events.EventObservationState.Initial => Contracts.Events.EventObservationState.Initial,
        Concepts.Events.EventObservationState.Replay => Contracts.Events.EventObservationState.Replay,
        _ => Contracts.Events.EventObservationState.None
    };
}
