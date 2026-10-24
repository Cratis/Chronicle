// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine;

public partial class KeyResolvers
{
    static KeyResolver WithParentHistoryBound(KeyResolver resolver) => async (storage, sink, @event) =>
    {
        using var scope = ParentKeyResolutionScope.ForChild(@event.Context.SequenceNumber);
        return await resolver(storage, sink, @event);
    };

    /// <summary>
    /// Resolves ancestors strictly before the original child, or through the releasing parent event
    /// inclusive for deferred children. Later parent-key changes in a replay page are excluded.
    /// </summary>
    /// <param name="parentKey">The parent key whose event source holds the parent events.</param>
    /// <param name="parentProjection">The parent projection.</param>
    /// <param name="eventSequenceStorage">The event sequence storage to read parent history from.</param>
    /// <param name="sink">The sink used when a parent candidate defers.</param>
    /// <returns>The parent event, if one exists within the boundary.</returns>
    async Task<Option<AppendedEvent>> TryResolveParentEventFromSequence(
        Key parentKey,
        IProjection parentProjection,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink)
    {
        EventSourceId source = parentKey.Value?.ToString()!;
        var types = parentProjection.OwnEventTypes.Select(type => type.Id).ToArray();
        var from = ParentKeyResolutionScope.Current.SequenceNumber;
        var inclusive = ParentKeyResolutionScope.Current.Inclusive;
        while (inclusive || from > EventSequenceNumber.First)
        {
            var upTo = inclusive ? from : from - 1;
            var history = await eventSequenceStorage.GetInstancesOfAnyInRange([source], types, from, upTo);
            if (!history.TryGetValue(source, out var candidates) || candidates.Count == 0)
            {
                break;
            }

            var candidate = candidates[^1];
            var resolved = await TryResolveParentCandidate(parentProjection, eventSequenceStorage, sink, candidate);
            if (resolved.HasValue)
            {
                return resolved;
            }

            // Fetch further back only after a candidate defers, never the whole source history.
            from = candidate.Context.SequenceNumber;
            inclusive = false;
        }
        return Option<AppendedEvent>.None();
    }

    async Task<Option<AppendedEvent>> TryResolveParentCandidate(IProjection projection, Storage.EventSequences.IEventSequenceStorage storage, Storage.Sinks.ISink sink, AppendedEvent candidate)
    {
        var eventType = projection.EventTypes.FirstOrDefault(type => type.Id == candidate.Context.EventType.Id);
        if (eventType is null)
        {
            return Option<AppendedEvent>.None();
        }
        if (projection.GenerationDelivery == EventGenerationDelivery.Pinned)
        {
            candidate = await projection.ReleaseEvent(eventType, candidate);
        }
        var result = await projection.GetKeyResolverFor(eventType)(storage, sink, candidate);
        if (result is ResolvedKey)
        {
            return new Option<AppendedEvent>(candidate);
        }
        logger.FromParentHierarchySkippingDeferredParentEvent(candidate.Context.SequenceNumber.Value);
        return Option<AppendedEvent>.None();
    }
}
