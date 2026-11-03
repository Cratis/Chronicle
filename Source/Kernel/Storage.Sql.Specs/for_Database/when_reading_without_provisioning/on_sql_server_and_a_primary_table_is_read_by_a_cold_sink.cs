// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

/// <summary>
/// The SQL Server fixture has a case-insensitive collation, so a table cannot hold <c language="csharp">Id</c> beside
/// <c language="csharp">ID</c> and the legacy layout cannot exist there. This pins that a cold sink still lists and
/// observes an existing primary table whose key is spelled <c language="csharp">Id</c> for an <c language="csharp">ID</c> schema.
/// </summary>
/// <param name="fixture">The <see cref="SqlServerFixture"/>.</param>
[Collection(SqlServerCollection.Name)]
public class on_sql_server_and_a_primary_table_is_read_by_a_cold_sink(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    OccurrenceRead _primary;

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        var connection = ConnectionFor(await ReadModelDatabaseName());
        await Execute(connection, $"CREATE TABLE [indexed_model] ([Id] nvarchar(450) NOT NULL PRIMARY KEY, [value] int NULL, [{WellKnownProperties.LastHandledEventSequenceNumber}] bigint NULL, [{WellKnownProperties.ReadModelInstanceInitialized}] bit NOT NULL DEFAULT 1)");
        await Execute(connection, "INSERT INTO [indexed_model] ([Id], [value]) VALUES ('one', 1), ('two', 2)");
    }

    async Task Because() => _primary = await ColdIndexedSink.ReadThePrimaryTable(Database, ColdIndexedSink.UpperCaseIdentifierSchema, Store, Namespace);

    [Fact] void should_list_and_observe_the_rows_of_the_primary_table() => _primary.ShouldEqual(new OccurrenceRead(2, 2, 2));
}
