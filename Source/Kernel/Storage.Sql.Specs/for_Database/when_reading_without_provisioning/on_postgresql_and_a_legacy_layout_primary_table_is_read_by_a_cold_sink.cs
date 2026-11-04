// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

/// <summary>
/// Earlier releases created the tables with a synthesized <c language="csharp">Id</c> key beside a nullable <c language="csharp">ID</c> column.
/// A sink that was never initialized has to read the primary table in that layout, through every query that opens it.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/>.</param>
[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_a_legacy_layout_primary_table_is_read_by_a_cold_sink(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    OccurrenceRead _primary;
    object? _found;
    object? _foundInPrimary;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        var connection = await ReadModelConnection();
        await Execute(connection, $"CREATE TABLE \"indexed_model\" (\"Id\" text NOT NULL PRIMARY KEY, \"ID\" text NULL, \"value\" integer NULL, \"{WellKnownProperties.LastHandledEventSequenceNumber}\" bigint NULL, \"{WellKnownProperties.ReadModelInstanceInitialized}\" boolean NOT NULL DEFAULT true)");
        await Execute(connection, "INSERT INTO \"indexed_model\" (\"Id\", \"value\") VALUES ('one', 1), ('two', 2)");
    }

    async Task Because()
    {
        _primary = await ColdIndexedSink.ReadThePrimaryTable(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace);
        _found = await ColdIndexedSink.Cold(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace, async sink => (object?)await sink.FindOrDefault(new Key("one", ArrayIndexers.NoIndexers)));
        _foundInPrimary = await ColdIndexedSink.Cold(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace, async sink => (object?)await sink.FindInPrimaryOrDefault(new Key("two", ArrayIndexers.NoIndexers)));
    }

    [Fact] void should_list_and_observe_the_rows_of_the_primary_table() => _primary.ShouldEqual(new OccurrenceRead(2, 2, 2));
    [Fact] void should_find_a_row_in_the_active_table() => _found.ShouldNotBeNull();
    [Fact] void should_find_a_row_in_the_primary_table() => _foundInPrimary.ShouldNotBeNull();
}
