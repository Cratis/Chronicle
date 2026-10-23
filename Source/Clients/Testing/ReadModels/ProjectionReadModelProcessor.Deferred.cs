// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Dynamic;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Testing.ReadModels;

internal static partial class ProjectionReadModelProcessor
{
    /// <summary>
    /// Retries deferred roots and child branches using the releasing parent's sequence, inclusive.
    /// </summary>
    /// <remarks>
    /// All siblings and nested retries share this boundary. Retry after each processed event, not after
    /// the replay page, so a later parent-key change cannot alter the key selected at release time.
    /// Ordinary resolution outside this scope remains strictly before the original child event.
    /// </remarks>
    /// <param name="projection">The projection being processed.</param>
    /// <param name="storage">The event sequence storage holding the scenario's events.</param>
    /// <param name="sink">The in-memory sink holding read model state.</param>
    /// <param name="releasingEvent">The parent event that releases the deferred events.</param>
    /// <param name="statesByKey">The read model states, by key.</param>
    /// <param name="createSeedState">Creates the seed state for a new root.</param>
    /// <param name="deferredEvents">The deferred root events.</param>
    /// <param name="deferredChildren">The deferred child events, with their child projections.</param>
    /// <param name="preserveIdentifiers">Restores seeded identifiers after a release.</param>
    /// <returns>The released root key, if any, and whether the release removed the root.</returns>
    static async Task<(KernelKey? Key, bool Removed)> ReleaseDeferredEvents(
        KernelProjectionEngine::IProjection projection,
        IEventSequenceStorage storage,
        InMemorySink sink,
        KernelAppendedEvent releasingEvent,
        Dictionary<object, ExpandoObject> statesByKey,
        Func<ExpandoObject> createSeedState,
        Queue<KernelAppendedEvent> deferredEvents,
        List<(KernelProjectionEngine::IProjection Child, KernelAppendedEvent Event)> deferredChildren,
        Action preserveIdentifiers)
    {
        using var scope = KernelProjectionEngine::ParentKeyResolutionScope.ForDeferredRelease(releasingEvent.Context.SequenceNumber);
        KernelKey? key = null;
        var removed = false;
        var resolvedAny = true;
        while (resolvedAny && (deferredEvents.Count > 0 || deferredChildren.Count > 0))
        {
            resolvedAny = false;
            var pendingCount = deferredEvents.Count;
            for (var i = 0; i < pendingCount; i++)
            {
                var @event = deferredEvents.Dequeue();
                var result = await projection.GetKeyResolverFor(@event.Context.EventType)(storage, sink, @event);
                if (result is KernelProjectionEngine::DeferredKey)
                {
                    deferredEvents.Enqueue(@event);
                    continue;
                }

                if (result is not KernelProjectionEngine::ResolvedKey resolved)
                {
                    continue;
                }

                key ??= resolved.Key;
                removed |= await ApplyResolvedEvent(projection, storage, sink, @event, resolved.Key, statesByKey, createSeedState, deferredChildren);
                preserveIdentifiers();
                resolvedAny = true;
            }

            foreach (var (child, @event) in deferredChildren.ToArray())
            {
                var result = await child.GetKeyResolverFor(@event.Context.EventType)(storage, sink, @event);
                if (result is KernelProjectionEngine::DeferredKey)
                {
                    continue;
                }

                deferredChildren.Remove((child, @event));
                if (result is not KernelProjectionEngine::ResolvedKey resolved)
                {
                    continue;
                }

                removed |= await ApplyResolvedEvent(child, storage, sink, @event, resolved.Key, statesByKey, createSeedState, deferredChildren);
                preserveIdentifiers();
                resolvedAny = true;
            }
        }

        return (key, removed);
    }
}
