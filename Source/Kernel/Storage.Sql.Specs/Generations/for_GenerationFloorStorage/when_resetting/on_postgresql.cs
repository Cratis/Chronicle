// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.when_resetting;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_floor_storage(fixture)
{
    ulong _revision;
    int _history;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because()
    {
        await Database.Wipe();
        _revision = (await _storage.Get()).Revision;
        _history = (await _storage.GetHistory()).Count;
    }
    [Fact] void should_preserve_floor() => _revision.ShouldEqual(1UL);
    [Fact] void should_preserve_audit() => _history.ShouldEqual(1);
}
#endif
