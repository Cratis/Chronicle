// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.for_ClusterStorage;

public class when_reading_generation_floors : Generations.for_GenerationAdmission.given.a_provider
{
    ClusterStorage _clusterStorage;
    IReadOnlyDictionary<EventStoreName, GenerationFloor> _floors;
    GenerationFloor _expected;

    async Task Establish()
    {
        await Advance(2);
        _expected = (await _storage.ReadGenerationFloors())[_name];
        _clusterStorage = new(_registry);
    }

    async Task Because() => _floors = await _clusterStorage.ReadGenerationFloors();

    [Fact] void should_find_the_store_floor() => _floors.Keys.ShouldEqual([_name]);
    [Fact] void should_read_the_shared_registry_floor() => _floors[_name].ShouldEqual(_expected);
    [Fact] void should_not_create_a_store() => _registry.Names.ShouldEqual([_name]);
}
