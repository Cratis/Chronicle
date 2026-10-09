// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Appends events for several event sources with per-event named tags.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="Events">The events, each carrying its own named tags.</param>
/// <param name="CorrelationId">Optional correlation id.</param>
/// <param name="Tags">Optional legacy tags for the batch.</param>
/// <param name="Causation">Optional causation.</param>
/// <param name="CausedBy">Optional identity.</param>
/// <param name="ConcurrencyScopes">Optional per-source concurrency scopes.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyForEventSourcesWithNamedTags(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    IEnumerable<EventForEventSourceIdWithNamedTags> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    IEnumerable<EventSourceConcurrencyScope>? ConcurrencyScopes = default)
{
    /// <summary>
    /// Handles the batch through the existing append pipeline.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <returns>The append result.</returns>
    public Task<AppendManyResult> Handle(IGrainFactory grainFactory, RequestCausation causation, ICurrentPrincipalAccessor principalAccessor)
    {
        var events = Events.ToArray();
        var namedTags = events.Select(@event => @event.NamedTags.ToChronicleNamedTags()).ToArray();
        return new AppendManyForEventSources(
            EventStore,
            Namespace,
            EventSequenceId,
            events.Select(@event => new EventForEventSourceId(
                EventSourceId: @event.EventSourceId,
                EventSourceType: @event.EventSourceType,
                EventStreamType: @event.EventStreamType,
                EventStreamId: @event.EventStreamId,
                EventType: @event.EventType,
                Content: @event.Content,
                Tags: @event.Tags,
                Occurred: @event.Occurred,
                Subject: @event.Subject,
                Causation: @event.Causation,
                EventSource: @event.EventSource)).ToArray(),
            CorrelationId,
            Tags,
            Causation,
            CausedBy,
            ConcurrencyScopes)
            .HandleWithNamedTags(grainFactory, causation, principalAccessor, namedTags);
    }
}
