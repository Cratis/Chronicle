// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Appends events belonging to one source with per-event named tags.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The source id.</param>
/// <param name="Events">The events, each carrying its own named tags.</param>
/// <param name="CorrelationId">Optional correlation id.</param>
/// <param name="Tags">Optional legacy tags for the batch.</param>
/// <param name="Occurred">Optional occurrence time for the batch.</param>
/// <param name="Causation">Optional causation.</param>
/// <param name="CausedBy">Optional identity.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyWithNamedTags(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    IEnumerable<EventToAppendWithNamedTags> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default)
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
        return new AppendMany(
            EventStore,
            Namespace,
            EventSequenceId,
            EventSourceId,
            events.Select(@event => new EventToAppend(@event.EventType, @event.Content, @event.Subject)).ToArray(),
            CorrelationId,
            Tags,
            Occurred,
            Causation,
            CausedBy,
            ConcurrencyScope)
            .HandleWithNamedTags(grainFactory, causation, principalAccessor, namedTags);
    }
}
