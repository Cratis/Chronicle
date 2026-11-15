// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Generations.Bindings;

/// <summary>Resolves materializations from the authoritative floor and definitions.</summary>
/// <param name="storage">The durable storage.</param>
/// <param name="sinkConfigurations">The <see cref="ISinkConfigurations"/> resolving the sink a read model stores to; none configured when omitted.</param>
public sealed class MaterializationKeys(IStorage storage, ISinkConfigurations? sinkConfigurations = null) : IMaterializationKeys
{
    readonly ISinkConfigurations _sinkConfigurations = sinkConfigurations ?? DefaultSinkConfigurations.Instance;

    /// <inheritdoc/>
    public async Task<MaterializationKey> For(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection)
    {
        var eventStore = storage.GetEventStore(store);
        var floor = await eventStore.GenerationFloor.Get();
        if (floor is null || floor.Incarnation == StoreIncarnation.Unspecified)
        {
            throw new GenerationFloorNotEstablished(store);
        }
        var definition = await eventStore.Projections.Get(projection);
        var model = await eventStore.ReadModels.Get(definition.ReadModel);

        // The resolved sink, not the declared one: a read model moved to another sink - by its own name, the client's default or
        // the kernel's configured default - is another materialization. Without named sinks this resolves to the declared type
        // and the default name, which keeps the key every existing binding was stored under.
        var sink = _sinkConfigurations.ResolveFor(model, SinkConfigurationRules.IsGraph(model.Sink.Type));
        return new(floor.Incarnation, ns, projection, sink.Type, model.Sink.Configuration) { SinkName = sink.Name };
    }
}
