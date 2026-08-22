// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending a single event to an event sequence.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The stream type within the event source.</param>
/// <param name="EventStreamId">The stream within the stream type.</param>
/// <param name="EventType">The type of event being appended.</param>
/// <param name="Content">The content of the event.</param>
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with the event.</param>
/// <param name="Occurred">Optional occurred time. If null, the server sets it to approximately the time of append.</param>
/// <param name="Subject">Optional subject identifying the compliance target for the event. Defaults to the event source.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <remarks>
/// Deliberately has no concurrency scope parameter yet: <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScope"/>
/// nests <c>IEnumerable&lt;Concepts.Events.EventType&gt;</c>, and mirroring that composite shape through
/// <c>SharedTypeRegistry</c> regenerates <c>Contracts.Events.EventType</c> and
/// <c>Contracts.EventSequences.Concurrency.ConcurrencyScope</c> in place - the same hand-written files every other
/// still-hand-written Contracts area (and the production client SDK's own converters) currently depend on. Wiring
/// it needs the same verification rigor Phase 2 applied to <c>JobStatus</c> (proto diff, WireCompatibility, every
/// consumer checked) before it is safe, not a parameter addition alongside the rest of this parity pass.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record Append(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    JsonObject Content,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Handles the command by appending the event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="AppendRejected">Thrown when the append is rejected by the kernel.</exception>
    internal async Task Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var result = await eventSequence.Append(
            (EventSourceType)EventSourceType,
            EventSourceId,
            (EventStreamType)EventStreamType,
            (EventStreamId)EventStreamId,
            EventType.ToChronicle(),
            Content,
            CorrelationId ?? Guid.NewGuid(),
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity(),
            (Tags ?? []).Select(tag => (Tag)tag),
            Concepts.EventSequences.Concurrency.ConcurrencyScope.None,
            Occurred,
            string.IsNullOrWhiteSpace(Subject) ? null : new Subject(Subject));

        AppendRejected.ThrowIfRejected(result.Errors, result.ConstraintViolations);
    }
}
