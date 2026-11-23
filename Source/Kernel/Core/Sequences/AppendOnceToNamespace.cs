// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences;

/// <summary>Appends one keyed event only to the namespace identity captured by the writer.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace name, resolved without creating it.</param>
/// <param name="ExpectedIncarnation">The captured identity. It is never refreshed during append.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The source id.</param>
/// <param name="EventSourceType">The source type.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream id.</param>
/// <param name="EventType">The event type.</param>
/// <param name="Content">The JSON content.</param>
/// <param name="IdempotencyKey">The key identifying this append intent.</param>
/// <param name="NamedTags">Optional structured named tags.</param>
/// <param name="CorrelationId">Optional correlation id.</param>
/// <param name="Tags">Optional legacy tags.</param>
/// <param name="Occurred">Optional occurrence time.</param>
/// <param name="Subject">Optional subject.</param>
/// <param name="Causation">Optional causation.</param>
/// <param name="CausedBy">Optional identity.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope.</param>
/// <param name="EventSource">Optional registered event source definition.</param>
/// <remarks>A separate RPC makes an older server fail closed rather than ignore the identity.</remarks>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendOnceToNamespace(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    NamespaceIncarnationId ExpectedIncarnation,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventType EventType,
    string Content,
    string IdempotencyKey,
    IEnumerable<NamedTag>? NamedTags = default,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default,
    string? EventSource = default)
{
    /// <summary>Resolves only the canonical name and dispatches with the original identity.</summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <param name="incarnations">The opt-in namespace capability.</param>
    /// <returns>The fenced append result.</returns>
    /// <exception cref="NamespaceIdentityRequired">Thrown when no captured namespace identity is supplied.</exception>
    public async Task<AppendOnceResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor,
        INamespaceIncarnations incarnations)
    {
        if (ExpectedIncarnation is null || ExpectedIncarnation.Value == Guid.Empty)
        {
            throw new NamespaceIdentityRequired(EventStore, Namespace);
        }

        var binding = await NamespaceBinding.LookupNamespaceBinding(EventStore, Namespace, incarnations);
        return await ToAppend(binding.Namespace).Handle(grainFactory, causation, principalAccessor);
    }

    /// <summary>Creates the underlying append for the canonical namespace, carrying the captured identity.</summary>
    /// <param name="canonical">The canonical namespace name.</param>
    /// <returns>The append command bound to <see cref="ExpectedIncarnation"/>.</returns>
    internal AppendOnceWithNamedTags ToAppend(EventStoreNamespaceName canonical) => new(
        EventStore,
        canonical,
        EventSequenceId,
        EventSourceId,
        EventSourceType,
        EventStreamType,
        EventStreamId,
        EventType,
        Content,
        IdempotencyKey,
        NamedTags ?? [],
        CorrelationId,
        Tags,
        Occurred,
        Subject,
        Causation,
        CausedBy,
        ConcurrencyScope,
        EventSource,
        ExpectedIncarnation);
}
