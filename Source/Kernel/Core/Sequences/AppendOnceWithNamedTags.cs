// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending a single event with named tags once for an idempotency key.
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
/// <param name="NamedTags">The structured named tags.</param>
/// <param name="CorrelationId">Optional correlation identifier.</param>
/// <param name="Tags">The tags to associate with the event.</param>
/// <param name="Occurred">Optional occurred time.</param>
/// <param name="Subject">Optional subject identifying the compliance target for the event.</param>
/// <param name="Causation">Optional caller-supplied causation chain.</param>
/// <param name="CausedBy">Optional caller-supplied identity.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope, checked only for a new append.</param>
/// <param name="EventSource">Optional name of the registered event source definition.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendOnceWithNamedTags(
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
    IEnumerable<NamedTag> NamedTags,
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
    /// <returns>The <see cref="AppendOnceResult"/> describing the settlement.</returns>
    public Task<AppendOnceResult> Handle(IGrainFactory grainFactory, RequestCausation causation, ICurrentPrincipalAccessor principalAccessor) =>
        new AppendOnce(
            EventStore,
            Namespace,
            EventSequenceId,
            EventSourceId,
            EventSourceType,
            EventStreamType,
            EventStreamId,
            EventType,
            Content,
            IdempotencyKey,
            CorrelationId,
            Tags,
            Occurred,
            Subject,
            Causation,
            CausedBy,
            ConcurrencyScope,
            EventSource)
            .HandleWithNamedTags(grainFactory, causation, principalAccessor, NamedTags.ToChronicleNamedTags());
}
