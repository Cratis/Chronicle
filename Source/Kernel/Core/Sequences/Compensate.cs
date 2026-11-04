// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending a compensating event for an event that is already in an event sequence.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the event being compensated for.</param>
/// <param name="CompensationFor">The sequence number of the event being compensated for.</param>
/// <param name="EventType">The type of the compensating event. It must be declared as a compensation for the type of the event being compensated.</param>
/// <param name="Content">The content of the compensating event, as serialized JSON.</param>
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with the event.</param>
/// <param name="Occurred">Optional occurred time. If null, the server sets it to approximately the time of append.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional identity for application callers. Chronicle users are always recorded as the current principal.</param>
/// <remarks>
/// A compensation works like an append, except the event source, event stream and subject come from the event being
/// compensated for rather than from the caller. The original event is never changed, and the new event records the
/// original's sequence number as its compensation target.
/// <para>
/// Content travels as a JSON string rather than <see cref="JsonObject"/> because protobuf-net has no serializer for
/// that BCL type.
/// </para>
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record Compensate(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    ulong CompensationFor,
    EventType EventType,
    string Content,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Handles the command by appending the compensating event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is compensating.</param>
    /// <returns>The <see cref="AppendResult"/> describing the outcome, successful or not.</returns>
    /// <remarks>
    /// A compensation that targets a missing event, or uses a type that does not compensate the target's type, is a
    /// rejected append reported on the returned <see cref="AppendResult"/>, not an exception.
    /// </remarks>
    public Task<AppendResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var correlationId = CorrelationId ?? Guid.NewGuid();
        return eventSequence.Compensate(
            new Concepts.Events.EventSequenceNumber(CompensationFor),
            EventType.ToChronicle(),
            JsonNode.Parse(Content)!.AsObject(),
            correlationId,
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            principalAccessor.Current.ResolveCausedBy(CausedBy),
            (Tags ?? []).Select(tag => (Tag)tag),
            Occurred).WithUnknownOutcome(correlationId);
    }
}
