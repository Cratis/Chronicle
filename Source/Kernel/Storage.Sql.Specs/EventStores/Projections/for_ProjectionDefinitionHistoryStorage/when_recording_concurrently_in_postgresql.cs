// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_recording_concurrently_in_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_history_storage(fixture)
{
    async Task Because()
    {
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => _storage.Record(_entry)));
        await Read();
    }
    [Fact] void should_keep_one_entry() => _entries.Length.ShouldEqual(1);
}
