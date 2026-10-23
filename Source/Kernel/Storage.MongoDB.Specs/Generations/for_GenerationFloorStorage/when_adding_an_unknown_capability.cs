// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_adding_an_unknown_capability(ReplicaSetMongoDBFixture fixture) : given.a_floor_storage(fixture)
{
    GenerationFloor _persisted;
    async Task Because()
    {
        var target = new GenerationFloor(new(1), new(1), [new("z.future"), GenerationCapability.FloorAdmission, new("z.future")], _floor.Incarnation, 0, DateTimeOffset.MinValue, "", "");
        await _storage.Advance(Request(target));
        _persisted = await CreateStorage().Get();
    }
    [Fact] void should_preserve_unknown_capabilities() => _persisted.RequiredCapabilities.Select(_ => _.Value).ShouldEqual(["FloorAdmission", "z.future"]);
}
