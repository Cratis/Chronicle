// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.when_the_target_is_already_satisfied;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_floor_storage(fixture)
{
    GenerationFloorAdvanceResult _result;
    GenerationFloor _before;
    GenerationFloor _after;
    IReadOnlyList<GenerationFloor> _history;

    async Task Establish()
    {
        await _storage.Advance(Request());
        _before = await _storage.Get();
    }

    async Task Because()
    {
        var target = new GenerationFloor(new(0), new(0), [], _floor.Incarnation, 0, DateTimeOffset.MinValue, "", "");
        _result = await _storage.Advance(Request(target));
        _after = await _storage.Get();
        _history = await _storage.GetHistory();
    }

    [Fact] void should_return_the_already_satisfied_outcome() => _result.Value.ShouldBeOfExactType<GenerationFloorAlreadyAtOrAbove>();
    [Fact] void should_keep_the_revision() => _after.Revision.ShouldEqual(_before.Revision);
    [Fact] void should_keep_the_storage_protocol() => _after.MinimumStorageProtocol.ShouldEqual(_before.MinimumStorageProtocol);
    [Fact] void should_keep_the_writer_protocol() => _after.MinimumWriterProtocol.ShouldEqual(_before.MinimumWriterProtocol);
    [Fact] void should_keep_the_capabilities() => _after.RequiredCapabilities.ShouldEqual(_before.RequiredCapabilities);
    [Fact] void should_not_append_history() => _history.Count.ShouldEqual(1);
}
