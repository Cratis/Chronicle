// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Manually completes a nonempty event stream scope.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The optional event source identifier.</param>
/// <param name="EventSourceType">The optional event source type.</param>
/// <param name="EventStreamType">The optional stream type.</param>
/// <param name="EventStreamId">The optional stream identifier.</param>
/// <param name="ExpectedTailSequenceNumber">The optional expected scope tail.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record CompleteStreamScope(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    string? EventSourceId = default,
    string? EventSourceType = default,
    string? EventStreamType = default,
    string? EventStreamId = default,
    ulong? ExpectedTailSequenceNumber = default)
{
    /// <summary>
    /// Complete the scope through its event sequence grain.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The completion outcome.</returns>
    /// <remarks>
    /// A covering manual closure returns AlreadyCompleted. Event-owned coverage alone still creates a
    /// manual closure, making the operator's decision independent of later reopening events.
    /// </remarks>
    public async Task<CompleteStreamOutcome> Handle(IGrainFactory grainFactory)
    {
        var sequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var scope = ClosedStreamConverters.ToScope(EventSourceId, EventSourceType, EventStreamType, EventStreamId);
        var result = await sequence.CompleteStream(scope, ExpectedTailSequenceNumber is null ? null : new EventSequenceNumber(ExpectedTailSequenceNumber.Value));

        if (!result.TryGetError(out var error)) return new(true, result.AsT0, CompleteStreamError.None);
        var localError = error switch
        {
            EventSequences.CompleteStreamError.AlreadyCompleted => CompleteStreamError.AlreadyCompleted,
            EventSequences.CompleteStreamError.DefaultStreamCannotBeCompleted => CompleteStreamError.DefaultStreamCannotBeCompleted,
            EventSequences.CompleteStreamError.EmptyScope => CompleteStreamError.EmptyScope,
            EventSequences.CompleteStreamError.ExpectedTailMismatch => CompleteStreamError.ExpectedTailMismatch,
            _ => CompleteStreamError.None
        };

        return new(false, EventSequenceNumber.Unavailable, localError);
    }
}
