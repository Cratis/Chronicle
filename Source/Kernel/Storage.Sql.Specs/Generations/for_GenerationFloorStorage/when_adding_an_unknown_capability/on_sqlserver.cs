// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.when_adding_an_unknown_capability;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_floor_storage(fixture)
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
