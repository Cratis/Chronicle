// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for completing a stream so that no further events can be appended to it.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the stream.</param>
/// <param name="EventStreamType">The stream type to complete.</param>
/// <param name="EventStreamId">The stream within the stream type to complete.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record CompleteStream(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventStreamType,
    string EventStreamId)
{
    /// <summary>
    /// Handles the command by completing the stream.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to complete the stream through.</param>
    /// <returns>The <see cref="CompleteStreamOutcome"/> describing the outcome, successful or not.</returns>
    /// <remarks>
    /// A stream that cannot be completed (already completed, or the default stream) is a normal, expected outcome
    /// - not an exceptional condition - so it is reported on the returned <see cref="CompleteStreamOutcome"/>
    /// rather than thrown.
    /// </remarks>
    internal async Task<CompleteStreamOutcome> Handle(IGrainFactory grainFactory)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var result = await eventSequence.CompleteStream(EventStreamType, EventStreamId);

        return result.TryGetError(out var error)
            ? new CompleteStreamOutcome(false, EventSequenceNumber.Unavailable, ToLocalError(error))
            : new CompleteStreamOutcome(true, result.AsT0, CompleteStreamError.None);
    }

    static CompleteStreamError ToLocalError(Cratis.Chronicle.EventSequences.CompleteStreamError error) => error switch
    {
        Cratis.Chronicle.EventSequences.CompleteStreamError.AlreadyCompleted => CompleteStreamError.AlreadyCompleted,
        Cratis.Chronicle.EventSequences.CompleteStreamError.DefaultStreamCannotBeCompleted => CompleteStreamError.DefaultStreamCannotBeCompleted,
        _ => CompleteStreamError.None
    };
}
