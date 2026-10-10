// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Repairs an exact manually completed scope, recording the operator and reason before reopening it.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="Reason">Why the scope is being reopened.</param>
/// <param name="EventSourceId">The optional event source identifier.</param>
/// <param name="EventSourceType">The optional event source type.</param>
/// <param name="EventStreamType">The optional stream type.</param>
/// <param name="EventStreamId">The optional stream identifier.</param>
/// <param name="Causation">Optional caller-supplied causation.</param>
/// <param name="CausedBy">Optional supplied identity. The repair audit always uses the authenticated principal instead.</param>
[Command]
[Authorize]
[BelongsTo(WellKnownServices.EventSequences)]
public record ReopenStreamScope(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    string Reason,
    string? EventSourceId = default,
    string? EventSourceType = default,
    string? EventStreamType = default,
    string? EventStreamId = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Reopens the scope through its grain. The actor is always the authenticated principal.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <returns>The repair outcome.</returns>
    public async Task<ReopenStreamScopeOutcome> Handle(IGrainFactory grainFactory, RequestCausation causation, ICurrentPrincipalAccessor principalAccessor)
    {
        var sequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var scope = ClosedStreamConverters.ToScope(EventSourceId, EventSourceType, EventStreamType, EventStreamId);
        var result = await sequence.ReopenCompletedStream(scope, Reason, CorrelationId.New(), Causation?.ToChronicle() ?? causation.GetCurrentChain(), principalAccessor.Current.ToIdentity());

        return result.TryGetError(out var error) ? new(false, error) : new(true, ReopenStreamScopeError.None);
    }
}
