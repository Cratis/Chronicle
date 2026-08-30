// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for redacting a single event.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the event.</param>
/// <param name="SequenceNumber">The sequence number of the event to redact.</param>
/// <param name="Reason">Why the event is being redacted.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <remarks>
/// Redaction removes the payload, not the event - the sequence slot and its context stay, so nothing downstream
/// has to cope with a hole where an event used to be.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record Redact(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    ulong SequenceNumber,
    string Reason,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Handles the command by requesting the redaction of the event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append the redaction request through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is redacting.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A reactor picks up the resulting <see cref="EventRedactionRequested"/> and performs the actual in-place
    /// replacement, rather than the redaction happening synchronously here.
    /// </remarks>
    public Task Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(EventStore, Namespace);
        return systemEventSequence.Append(
            (EventSourceId)EventSequenceId.Value,
            new EventRedactionRequested(EventSequenceId, SequenceNumber, Reason),
            correlationId: Guid.NewGuid(),
            causation: Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            causedBy: CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity());
    }
}
