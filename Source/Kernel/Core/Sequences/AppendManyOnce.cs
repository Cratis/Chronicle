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
/// Represents the command for appending a batch in which keyed events are settled against their durable identities.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="Events">The events, each with its own event source and an optional idempotency key; an event without a key is always appended.</param>
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with every event in the batch.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional identity for application callers. Chronicle users are always recorded as the current principal.</param>
/// <param name="ConcurrencyScopes">Optional concurrency scopes, checked for the new remainder only.</param>
/// <remarks>
/// Like AppendManyForEventSources, the events carry their own event source, so one command serves both single-source
/// and multi-source keyed batches. Keyed appends travel on their own RPC so a kernel that does not know them answers
/// Unimplemented instead of ignoring the keys (decision 0012).
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyOnce(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    IEnumerable<KeyedEventForEventSourceId> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    IEnumerable<EventSourceConcurrencyScope>? ConcurrencyScopes = default)
{
    /// <summary>
    /// Handles the command by settling the batch.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>The <see cref="AppendManyOnceResult"/> describing the settlement, successful or not.</returns>
    public Task<AppendManyOnceResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor) => HandleWithNamedTags(grainFactory, causation, principalAccessor, null);

    /// <summary>
    /// Handles the batch with per-event named tags.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <param name="namedTagsPerEvent">Named tags, one collection per event.</param>
    /// <returns>The keyed batch result.</returns>
    internal async Task<AppendManyOnceResult> HandleWithNamedTags(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor,
        IReadOnlyList<IReadOnlyCollection<Concepts.Events.NamedTag>>? namedTagsPerEvent)
    {
        var correlationId = CorrelationId ?? Guid.NewGuid();
        var scopes = (ConcurrencyScopes ?? []).ToChronicle();
        if (scopes.TryGetError(out var error))
        {
            return Failed(correlationId, error);
        }

        var conjunctions = scopes.AsT0;
        if (conjunctions.HasRepeatedIds)
        {
            return Failed(correlationId, new AppendError("Keyed appends do not support several concurrency scopes for the same event source. Nothing was written."));
        }

        var globalTags = (Tags ?? []).Select(tag => (Tag)tag).ToArray();
        var keyed = Events.ToArray();
        var events = keyed.Select((entry, index) => ToEventToAppend(entry.Event, globalTags, namedTagsPerEvent is null ? [] : namedTagsPerEvent[index])).ToArray();
        var keys = keyed
            .Select(entry => string.IsNullOrEmpty(entry.IdempotencyKey) ? null : new Concepts.EventSequences.IdempotencyKey(entry.IdempotencyKey))
            .ToArray();
        var legacyScopes = new Concepts.EventSequences.Concurrency.ConcurrencyScopes(conjunctions.Entries.ToDictionary(_ => _.EventSourceId, _ => _.Scope));

        var settlement = await grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace).AppendManyWithIdempotencyKeys(
            events,
            keys,
            correlationId,
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            principalAccessor.Current.ResolveCausedBy(CausedBy),
            legacyScopes);

        return AppendManyOnceResult.From(settlement);
    }

    static AppendManyOnceResult Failed(Guid correlationId, AppendError error) => new()
    {
        CorrelationId = correlationId,
        Errors = [error]
    };

    static EventSequences.EventToAppend ToEventToAppend(EventForEventSourceId @event, Tag[] globalTags, IReadOnlyCollection<Concepts.Events.NamedTag> namedTags)
    {
        var route = AppendRoute.Resolve(@event.EventSourceType, @event.EventStreamType, @event.EventStreamId);

        return new EventSequences.EventToAppend(
            route.SourceType,
            @event.EventSourceId,
            route.StreamType,
            route.StreamId,
            @event.EventType.ToChronicle(),
            (@event.Tags ?? []).Select(tag => (Tag)tag).Concat(globalTags).Distinct(),
            JsonNode.Parse(@event.Content)!.AsObject(),
            @event.Occurred,
            Subject: string.IsNullOrWhiteSpace(@event.Subject) ? null : new Subject(@event.Subject))
        {
            NamedTags = namedTags,
            Causation = @event.Causation?.ToChronicle(),
            EventSource = string.IsNullOrEmpty(@event.EventSource) ? null : new EventSourceName(@event.EventSource)
        };
    }
}
