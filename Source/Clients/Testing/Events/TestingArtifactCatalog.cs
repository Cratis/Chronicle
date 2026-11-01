// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Owns immutable discovery for an equivalent application catalog, never live store state.
/// </summary>
internal sealed class TestingArtifactCatalog
{
    readonly object _sync = new();
    EventTypesDiscovery? _discovery;
    FrozenDictionary<EventType, string>? _schemas;
    EventTypeSeed? _seed;
    int _discoveryBuildCount;

    /// <summary>
    /// Gets the number of successful event discovery builds.
    /// </summary>
    internal int DiscoveryBuildCount => _discoveryBuildCount;

    /// <summary>
    /// Gets the model-bound source which returns independent wire-definition copies.
    /// </summary>
    internal CachedModelBoundProjectionsSource ModelBoundSource { get; } = new();

    /// <summary>
    /// Gets the source of independent fluent wire definitions.
    /// </summary>
    internal CachedFluentProjectionDefinitionsSource FluentSource { get; } = new();

    /// <summary>
    /// Gets the immutable reducer type descriptors.
    /// </summary>
    internal CachedReducerDescriptorsSource ReducerDescriptors { get; } = new();

    /// <summary>
    /// Discovers once and adopts immutable descriptors and store-local schemas thereafter.
    /// </summary>
    /// <param name="eventTypes">The store-local registry.</param>
    internal void DiscoverEventTypes(EventTypes eventTypes)
    {
        lock (_sync)
        {
            if (_discovery is null)
            {
                eventTypes.Discover().GetAwaiter().GetResult();
                var discovery = eventTypes.CaptureDiscovery();
                _schemas = discovery.Schemas.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.ToJson());
                _discovery = discovery with { Schemas = FrozenDictionary<EventType, JsonSchema>.Empty };
                _discoveryBuildCount++;
                return;
            }
        }

        eventTypes.AdoptDiscovery(_discovery with
        {
            Schemas = _schemas!.ToFrozenDictionary(pair => pair.Key, pair => JsonSchema.FromJson(pair.Value))
        });
    }

    /// <summary>
    /// Gets the frozen kernel seed, retrying construction if the factory fails.
    /// </summary>
    /// <param name="create">Builds the seed once.</param>
    /// <returns>The immutable seed.</returns>
    internal EventTypeSeed GetSeed(Func<EventTypeSeed> create)
    {
        lock (_sync)
        {
            return _seed ??= create();
        }
    }
}
