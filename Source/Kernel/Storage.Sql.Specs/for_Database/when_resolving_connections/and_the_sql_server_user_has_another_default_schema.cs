// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

[Collection(SqlServerCollection.Name)]
public class and_the_sql_server_user_has_another_default_schema(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    Exception? _error;

    async Task Because()
    {
        var database = new Database(
            Services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString, Schema = "chronicle" } }),
            new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
            new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
            new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance));
        try
        {
            await using var scope = await database.Cluster();
        }
        catch (DefaultSchemaMismatch ex)
        {
            _error = ex;
        }
    }

    [Fact] void should_refuse_to_migrate_outside_the_schema() => _error.ShouldNotBeNull();
    [Fact] void should_name_the_default_schema_of_the_user() => _error!.Message.ShouldContain("'dbo'");
}
