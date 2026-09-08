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
/// <param name="ConcurrencyScope">Optional concurrency scope to validate the append against. Defaults to no check when not provided.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record Append(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventType EventType,
    JsonObject Content,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default)
{
    /// <summary>
    /// Handles the command by appending the event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>The <see cref="AppendResult"/> describing the outcome, successful or not.</returns>
    /// <remarks>
    /// A rejected append (constraint or concurrency violation) is a normal, expected outcome of appending - not an
    /// exceptional condition - so it is reported on the returned <see cref="AppendResult"/> rather than thrown.
    /// </remarks>
    public Task<AppendResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        return eventSequence.Append(
            EventSourceType,
            EventSourceId,
            EventStreamType,
            EventStreamId,
            EventType.ToChronicle(),
            Content,
            CorrelationId ?? Guid.NewGuid(),
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity(),
            (Tags ?? []).Select(tag => (Tag)tag),
            ConcurrencyScope?.ToChronicle() ?? Concepts.EventSequences.Concurrency.ConcurrencyScope.None,
            Occurred,
            string.IsNullOrWhiteSpace(Subject) ? null : new Subject(Subject));
    }
}
