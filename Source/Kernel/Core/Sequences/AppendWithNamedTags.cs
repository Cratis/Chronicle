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
/// Appends one event with structured named tags. This dedicated RPC cannot be silently downgraded by an older server.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The source id.</param>
/// <param name="EventSourceType">The source type.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream id.</param>
/// <param name="EventType">The event type.</param>
/// <param name="Content">The JSON content.</param>
/// <param name="NamedTags">The named tags for this event.</param>
/// <param name="CorrelationId">Optional correlation id.</param>
/// <param name="Tags">Optional legacy tags.</param>
/// <param name="Occurred">Optional occurrence time.</param>
/// <param name="Subject">Optional subject.</param>
/// <param name="Causation">Optional causation.</param>
/// <param name="CausedBy">Optional identity.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendWithNamedTags(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventType EventType,
    string Content,
    IEnumerable<NamedTag> NamedTags,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default)
{
    /// <summary>
    /// Handles the named-tag append through the existing append pipeline.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <returns>The append result.</returns>
    public Task<AppendResult> Handle(IGrainFactory grainFactory, RequestCausation causation, ICurrentPrincipalAccessor principalAccessor) =>
        new Append(
            EventStore,
            Namespace,
            EventSequenceId,
            EventSourceId,
            EventSourceType,
            EventStreamType,
            EventStreamId,
            EventType,
            Content,
            CorrelationId,
            Tags,
            Occurred,
            Subject,
            Causation,
            CausedBy,
            ConcurrencyScope)
            .HandleWithNamedTags(grainFactory, causation, principalAccessor, NamedTags.ToChronicleNamedTags());
}
