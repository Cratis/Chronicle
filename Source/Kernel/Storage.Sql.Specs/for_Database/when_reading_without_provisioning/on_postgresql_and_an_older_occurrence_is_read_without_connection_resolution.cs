// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_an_older_occurrence_is_read_without_connection_resolution(PostgreSqlFixture fixture) : given.an_older_postgresql_occurrence(fixture)
{
    protected override Database Reader => Database;

    async Task Because()
    {
        _read = await ColdIndexedSink.ReadTheOccurrence(Reader, ColdIndexedSink.IntegerValueSchema, Store, Namespace);
        var connection = await ReadModelConnection();
        _catalogAfter = await Catalog(connection);
        _databasesAfter = await PhysicalDatabases();
    }

    [Fact] void should_read_nothing_and_change_nothing() =>
        (_rowsBefore == 1, _read, _catalogAfter == _catalogBefore, string.Join(',', _databasesAfter) == string.Join(',', _databasesBefore)).ShouldEqual((true, new OccurrenceRead(0, 0, 0), true, true));
}
