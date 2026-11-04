// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

/// <summary>
/// Earlier releases created the tables with a synthesized <c language="csharp">Id</c> key beside a nullable <c language="csharp">ID</c> column.
/// A sink that was never initialized has to read the occurrence and backup tables in that layout.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/>.</param>
[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_a_legacy_layout_occurrence_is_read_by_a_cold_sink(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    const string Backup = "indexed_model-20240101000001-cafebabe";

    OccurrenceRead _occurrence;
    OccurrenceRead _backup;
    string _catalogBefore;
    string _catalogAfter;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        var connection = await ReadModelConnection();
        foreach (var table in new[] { "indexed_model", ColdIndexedSink.Occurrence, Backup })
        {
            await Execute(connection, $"CREATE TABLE \"{table}\" (\"Id\" text NOT NULL PRIMARY KEY, \"ID\" text NULL, \"value\" integer NULL, \"{WellKnownProperties.LastHandledEventSequenceNumber}\" bigint NULL, \"{WellKnownProperties.ReadModelInstanceInitialized}\" boolean NOT NULL DEFAULT true)");
            await Execute(connection, $"INSERT INTO \"{table}\" (\"Id\", \"value\") VALUES ('one', 1), ('two', 2)");
        }

        _catalogBefore = await Catalog(connection);
    }

    async Task Because()
    {
        _occurrence = await ColdIndexedSink.ReadTheOccurrence(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace);
        _backup = await ColdIndexedSink.ReadTheOccurrence(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace, Backup);
        _catalogAfter = await Catalog(await ReadModelConnection());
    }

    [Fact] void should_read_the_rows_of_the_occurrence() => _occurrence.ShouldEqual(new OccurrenceRead(2, 2, 2));
    [Fact] void should_read_the_rows_of_the_backup() => _backup.ShouldEqual(new OccurrenceRead(2, 2, 2));
    [Fact] void should_not_change_the_catalog() => _catalogAfter.ShouldEqual(_catalogBefore);
}
