// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences;

/// <summary>Appends a keyed multi-source batch only in a captured namespace incarnation.</summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace to resolve without recreating it.</param>
/// <param name="ExpectedIncarnation">The required identity captured before the decision.</param>
/// <param name="EventSequenceId">The destination sequence.</param>
/// <param name="Events">The events and optional keys, retained in request order.</param>
/// <param name="CorrelationId">Optional correlation identifier.</param>
/// <param name="Tags">Optional legacy tags merged into each event.</param>
/// <param name="Causation">Optional batch causation chain.</param>
/// <param name="CausedBy">Optional causing identity.</param>
/// <param name="ConcurrencyScopes">Optional concurrency preconditions for the new remainder.</param>
/// <remarks>A separate RPC makes an older server fail closed rather than ignore the identity.</remarks>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyOnceToNamespace(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    NamespaceIncarnationId ExpectedIncarnation,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    IEnumerable<KeyedEventForEventSourceIdWithNamedTags> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    IEnumerable<EventSourceConcurrencyScope>? ConcurrencyScopes = default)
{
    /// <summary>Resolves only the canonical name and dispatches with the original identity.</summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="causation">The request causation.</param>
    /// <param name="principalAccessor">The current principal.</param>
    /// <param name="incarnations">The opt-in namespace capability.</param>
    /// <returns>The fenced batch result.</returns>
    /// <exception cref="NamespaceIdentityRequired">Thrown when no captured namespace identity is supplied.</exception>
    public async Task<AppendManyOnceResult> Handle(
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
    internal AppendManyOnceWithNamedTags ToAppend(EventStoreNamespaceName canonical) => new(
        EventStore,
        canonical,
        EventSequenceId,
        Events,
        CorrelationId,
        Tags,
        Causation,
        CausedBy,
        ConcurrencyScopes,
        ExpectedIncarnation);
}
