// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Holds what discovery produced for a catalog, as templates that a store copies instead of sharing.
/// </summary>
internal sealed class ApplicationMetadata
{
    readonly object _eventTypesSync = new();
    readonly object _seedSync = new();
    EventTypesDiscovery? _eventTypes;
    IReadOnlyList<KernelEvents::EventTypeDefinition>? _seedDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetadata"/> class.
    /// </summary>
    internal ApplicationMetadata() => ModelBoundSource = new CachedModelBoundProjectionsSource();

    /// <summary>
    /// Gets the source that serves model-bound definitions from the cache.
    /// </summary>
    internal IModelBoundProjectionsSource ModelBoundSource { get; }

    /// <summary>
    /// Discovers the event types for the registry, or adopts what an equivalent registry discovered before.
    /// </summary>
    /// <param name="eventTypes">The registry of the store being constructed.</param>
    internal void DiscoverEventTypes(EventTypes eventTypes)
    {
        var captured = Volatile.Read(ref _eventTypes);
        if (captured is null)
        {
            lock (_eventTypesSync)
            {
                if (_eventTypes is null)
                {
                    eventTypes.Discover().GetAwaiter().GetResult();
                    Volatile.Write(ref _eventTypes, eventTypes.CaptureDiscovery());
                    return;
                }

                captured = _eventTypes;
            }
        }

        // Adopting only clones from the captured templates, so concurrent stores do not need to wait for each other.
        eventTypes.AdoptDiscovery(captured).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the event type definitions to seed, computing them once.
    /// </summary>
    /// <param name="compute">Computes the definitions from the first store's inputs.</param>
    /// <returns>The definitions. They are templates: a store must register copies.</returns>
    internal IReadOnlyList<KernelEvents::EventTypeDefinition> GetSeedDefinitions(Func<IReadOnlyList<KernelEvents::EventTypeDefinition>> compute)
    {
        lock (_seedSync)
        {
            return _seedDefinitions ??= compute();
        }
    }
}
