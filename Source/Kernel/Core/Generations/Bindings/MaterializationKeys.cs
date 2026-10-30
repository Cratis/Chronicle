// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Bindings;

/// <summary>Resolves materializations from the authoritative floor and definitions.</summary>
/// <param name="storage">The durable storage.</param>
public sealed class MaterializationKeys(IStorage storage) : IMaterializationKeys
{
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
        return new(floor.Incarnation, ns, projection, model.Sink.Type, model.Sink.Configuration);
    }
}
