// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for revising the content of an already appended event.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the event.</param>
/// <param name="SequenceNumber">The sequence number of the event to revise.</param>
/// <param name="EventType">The type of the event being revised.</param>
/// <param name="Content">The revised content.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <remarks>
/// A revision does not rewrite history - the original content is kept alongside the revision, so what the event
/// said before stays answerable.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record Revise(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    ulong SequenceNumber,
    EventType EventType,
    JsonObject Content,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Handles the command by requesting the revision of the event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append the revision request through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is revising.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A reactor picks up the resulting <see cref="EventRevised"/> and performs the actual in-place update, rather
    /// than the revision happening synchronously here.
    /// </remarks>
    public Task Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(EventStore, Namespace);
        return systemEventSequence.Append(
            (EventSourceId)EventSequenceId.Value,
            new EventRevised(
                EventSequenceId,
                SequenceNumber,
                EventType.ToChronicle(),
                JsonSerializer.Serialize(Content)),
            correlationId: Guid.NewGuid(),
            causation: Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            causedBy: CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity());
    }
}
