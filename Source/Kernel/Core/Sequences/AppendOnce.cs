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
/// Represents the command for appending a single event once for an idempotency key.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The stream type within the event source.</param>
/// <param name="EventStreamId">The stream within the stream type.</param>
/// <param name="EventType">The type of event being appended.</param>
/// <param name="Content">The content of the event, as serialized JSON.</param>
/// <param name="IdempotencyKey">The key identifying the append intent.</param>
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with the event.</param>
/// <param name="Occurred">Optional occurred time. If null, the server sets it to approximately the time of append.</param>
/// <param name="Subject">Optional subject identifying the compliance target for the event. Defaults to the event source.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope, checked only for a new append.</param>
/// <param name="EventSource">Optional name of the registered event source definition the event is appended through.</param>
/// <remarks>
/// Keyed appends travel on their own RPC so a kernel that does not know them answers Unimplemented instead of
/// ignoring the key (decision 0012).
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendOnce(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventType EventType,
    string Content,
    string IdempotencyKey,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default,
    string? EventSource = default)
{
    /// <summary>
    /// Handles the command by appending the event once for its key.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>The <see cref="AppendOnceResult"/> describing the settlement, successful or not.</returns>
    public Task<AppendOnceResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor) => HandleWithNamedTags(grainFactory, causation, principalAccessor, []);

    /// <summary>
    /// Handles a keyed append with validated named tags.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <param name="namedTags">The structured named tags.</param>
    /// <returns>The keyed append result.</returns>
    internal async Task<AppendOnceResult> HandleWithNamedTags(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor,
        IReadOnlyCollection<Concepts.Events.NamedTag> namedTags)
    {
        var route = AppendRoute.Resolve(EventSourceType?.Value, EventStreamType?.Value, EventStreamId?.Value);
        var @event = new EventSequences.EventToAppend(
            route.SourceType,
            EventSourceId,
            route.StreamType,
            route.StreamId,
            EventType.ToChronicle(),
            (Tags ?? []).Select(tag => (Tag)tag),
            JsonNode.Parse(Content)!.AsObject(),
            Occurred,
            string.IsNullOrWhiteSpace(Subject) ? null : new Subject(Subject))
        {
            NamedTags = namedTags,
            EventSource = string.IsNullOrEmpty(EventSource) ? null : new EventSourceName(EventSource)
        };

        var settlement = await grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace).AppendWithIdempotencyKey(
            @event,
            new Concepts.EventSequences.IdempotencyKey(IdempotencyKey),
            CorrelationId ?? Guid.NewGuid(),
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity(),
            ConcurrencyScope?.ToChronicle() ?? Concepts.EventSequences.Concurrency.ConcurrencyScope.None);

        return AppendOnceResult.From(settlement);
    }
}
