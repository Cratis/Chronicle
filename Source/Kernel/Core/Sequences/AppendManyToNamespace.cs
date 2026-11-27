// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences;

/// <summary>Appends a batch for one source only to the namespace identity captured by the writer.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace name, resolved without creating it.</param>
/// <param name="ExpectedIncarnation">The captured identity, never refreshed during append.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The source id.</param>
/// <param name="Events">The events, with their individual named tags.</param>
/// <param name="CorrelationId">Optional correlation id.</param>
/// <param name="Tags">Optional legacy tags shared by the batch.</param>
/// <param name="Occurred">Optional occurrence time shared by the batch.</param>
/// <param name="Causation">Optional causation.</param>
/// <param name="CausedBy">Optional identity.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope.</param>
/// <param name="EffectReceipt">Optional effect to settle atomically.</param>
/// <remarks>A separate RPC makes an older server fail closed rather than ignore the identity.</remarks>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyToNamespace(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    NamespaceIncarnationId ExpectedIncarnation,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    IEnumerable<EventToAppendWithNamedTags> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default,
    Occurrences.EffectReceipt? EffectReceipt = default)
{
    /// <summary>Resolves only the canonical name and dispatches with the original identity.</summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <param name="incarnations">The opt-in namespace capability.</param>
    /// <returns>The fenced batch result.</returns>
    /// <exception cref="NamespaceIdentityRequired">Thrown when no captured namespace identity is supplied.</exception>
    public async Task<AppendManyResult> Handle(
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

    /// <summary>Creates the underlying batch for the canonical namespace with the captured identity.</summary>
    /// <param name="canonical">The canonical namespace name.</param>
    /// <returns>The batch command bound to <see cref="ExpectedIncarnation"/>.</returns>
    internal AppendManyWithNamedTags ToAppend(EventStoreNamespaceName canonical) => new(
        EventStore,
        canonical,
        EventSequenceId,
        EventSourceId,
        Events,
        CorrelationId,
        Tags,
        Occurred,
        Causation,
        CausedBy,
        ConcurrencyScope,
        EffectReceipt,
        ExpectedIncarnation);
}
