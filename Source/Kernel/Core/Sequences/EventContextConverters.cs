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
        ObservationState = context.ObservationState.ToContract(),
        Subject = context.Subject
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
        context.ObservationState)
    {
        Subject = context.Subject?.IsSet == true ? context.Subject.Value : context.EventSourceId.Value
    };

    /// <summary>
    /// Convert to contract version of the observation state.
    /// </summary>
    /// <param name="state">The state to convert.</param>
    /// <returns>The converted contract version.</returns>
    /// <remarks>
    /// Mapped flag by flag rather than by whole value: this is a [Flags] enum, so a switch on the exact value
    /// silently answers None for any combination - which is how CatchUp, which travels alongside Initial,
    /// would have been erased on the way to the client.
    /// </remarks>
    static Contracts.Events.EventObservationState ToContract(this Concepts.Events.EventObservationState state)
    {
        var result = Contracts.Events.EventObservationState.None;
        if (state.HasFlag(Concepts.Events.EventObservationState.Initial)) result |= Contracts.Events.EventObservationState.Initial;
        if (state.HasFlag(Concepts.Events.EventObservationState.Replay)) result |= Contracts.Events.EventObservationState.Replay;
        if (state.HasFlag(Concepts.Events.EventObservationState.CatchUp)) result |= Contracts.Events.EventObservationState.CatchUp;
        return result;
    }
}
