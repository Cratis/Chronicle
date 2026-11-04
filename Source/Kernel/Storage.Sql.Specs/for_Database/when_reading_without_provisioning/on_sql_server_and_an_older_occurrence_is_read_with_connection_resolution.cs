// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(SqlServerCollection.Name)]
public class on_sql_server_and_an_older_occurrence_is_read_with_connection_resolution(SqlServerFixture fixture) : given.an_older_sql_server_occurrence(fixture)
{
    protected override Database Reader => ResolvingDatabase();

    async Task Because()
    {
        _read = await ColdIndexedSink.ReadTheOccurrence(Reader, ColdIndexedSink.IntegerValueSchema, Store, Namespace);
        _catalogAfter = await Catalog(ConnectionFor(await ReadModelDatabaseName()));
        _databasesAfter = await PhysicalDatabases();
    }

    [Fact] void should_read_nothing_and_change_nothing() =>
        (_catalogBefore.Contains($"{ColdIndexedSink.Occurrence}.id", StringComparison.Ordinal), _read, _catalogAfter == _catalogBefore, string.Join(',', _databasesAfter) == string.Join(',', _databasesBefore)).ShouldEqual((true, new OccurrenceRead(0, 0, 0), true, true));
}
