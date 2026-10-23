// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.for_ClusterStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_generation_floors(ReplicaSetMongoDBFixture fixture) : given.a_cluster_storage_with_generation_floors(fixture)
{
    IReadOnlyDictionary<EventStoreName, GenerationFloor> _floors;

    async Task Because() => _floors = await _clusterStorage.ReadGenerationFloors();

    [Fact] void should_find_the_registered_store_floor() => _floors[_name].Revision.ShouldEqual(1UL);
    [Fact] void should_find_the_unregistered_store_floor() => _floors[_unregistered].Revision.ShouldEqual(1UL);
    [Fact] void should_find_the_system_floor() => _floors[EventStoreName.System].Revision.ShouldEqual(1UL);
    [Fact] void should_read_the_stores_in_ordinal_order() => _floors.Keys.ShouldEqual(new[] { _name, _unregistered, EventStoreName.System }.OrderBy(_ => _.Value, StringComparer.Ordinal));
    [Fact] async Task should_not_register_discovered_stores() => (await RegisteredStores()).ShouldEqual([_name]);
    [Fact] async Task should_not_create_a_database() => (await Databases()).ShouldEqual(_before);
}
