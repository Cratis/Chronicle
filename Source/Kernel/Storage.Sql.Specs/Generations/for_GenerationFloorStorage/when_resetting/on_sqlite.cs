// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using GenerationFloorStorage = Cratis.Chronicle.Storage.Sql.EventStores.Generations.GenerationFloorStorage;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.when_resetting;

public class on_sqlite : for_Database.given.a_sqlite_database
{
    GenerationFloorStorage _storage;
    ulong _revision;
    int _history;

    async Task Establish()
    {
        _storage = new(() => _database.EventStore("orders"));
        var floor = new GenerationFloor(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
        await _storage.Advance(new(floor, 0, "spec", "reset preservation", new(true, "spec members")));
    }

    async Task Because()
    {
        await _database.Wipe();
        _revision = (await _storage.Get()).Revision;
        _history = (await _storage.GetHistory()).Count;
    }

    [Fact] void should_preserve_floor() => _revision.ShouldEqual(1UL);
    [Fact] void should_preserve_audit() => _history.ShouldEqual(1);
}
#endif
