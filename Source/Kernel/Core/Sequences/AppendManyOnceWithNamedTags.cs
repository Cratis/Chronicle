// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending a keyed batch whose events carry named tags.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="Events">The events, each with its own event source and an optional idempotency key.</param>
/// <param name="CorrelationId">Optional correlation identifier.</param>
/// <param name="Tags">The tags to associate with every event in the batch.</param>
/// <param name="Causation">Optional caller-supplied causation chain.</param>
/// <param name="CausedBy">Optional caller-supplied identity.</param>
/// <param name="ConcurrencyScopes">Optional concurrency scopes, checked for the new remainder only.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyOnceWithNamedTags(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    IEnumerable<KeyedEventForEventSourceIdWithNamedTags> Events,
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
    /// <returns>The <see cref="AppendManyOnceResult"/> describing the settlement.</returns>
    public Task<AppendManyOnceResult> Handle(IGrainFactory grainFactory, RequestCausation causation, ICurrentPrincipalAccessor principalAccessor)
    {
        var events = Events.ToArray();
        var namedTags = events.Select(entry => entry.Event.NamedTags.ToChronicleNamedTags()).ToArray();
        return new AppendManyOnce(
            EventStore,
            Namespace,
            EventSequenceId,
            events.Select(entry => new KeyedEventForEventSourceId(
                new EventForEventSourceId(
                    EventSourceId: entry.Event.EventSourceId,
                    EventSourceType: entry.Event.EventSourceType,
                    EventStreamType: entry.Event.EventStreamType,
                    EventStreamId: entry.Event.EventStreamId,
                    EventType: entry.Event.EventType,
                    Content: entry.Event.Content,
                    Tags: entry.Event.Tags,
                    Occurred: entry.Event.Occurred,
                    Subject: entry.Event.Subject,
                    Causation: entry.Event.Causation,
                    EventSource: entry.Event.EventSource),
                entry.IdempotencyKey)).ToArray(),
            CorrelationId,
            Tags,
            Causation,
            CausedBy,
            ConcurrencyScopes)
            .HandleWithNamedTags(grainFactory, causation, principalAccessor, namedTags);
    }
}
