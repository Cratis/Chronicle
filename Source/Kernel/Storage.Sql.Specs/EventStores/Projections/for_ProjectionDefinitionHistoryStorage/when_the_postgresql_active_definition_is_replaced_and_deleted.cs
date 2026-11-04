// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_the_postgresql_active_definition_is_replaced_and_deleted(PostgreSqlFixture fixture) : given.a_postgresql_history_storage(fixture)
{
    ProjectionDefinition _latest;
    async Task Because()
    {
        var active = new ProjectionDefinitionsStorage(Store, Database);
        await _storage.Record(_entry);
        await active.Save(_entry.Definition);
        var changed = _entry with { Digest = new string('B', 64), Definition = _entry.Definition with { IsActive = false } };
        await _storage.Record(changed);
        await active.Save(changed.Definition);
        _latest = await active.Get(_entry.Projection);
        await active.Delete(_entry.Projection);
        await Read();
    }
    [Fact] void should_read_the_latest_active_definition() => _latest.IsActive.ShouldBeFalse();
    [Fact] void should_retain_both_history_snapshots() => _entries.Length.ShouldEqual(2);
}
