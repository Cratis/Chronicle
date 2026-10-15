// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_sql_server_database(SqlServerFixture fixture) : Specification
{
    internal string ConnectionString;
    internal Database Database;
    internal ServiceProvider Services;
    internal string Store = "a-store";

    async Task Establish()
    {
        ConnectionString = await fixture.CreateDatabase();
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance);
        Services = new ServiceCollection().AddSingleton<IReadModelMigrator>(migrator).BuildServiceProvider();
        Database = new(
            Services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString } }),
            new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
            new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
            migrator);
    }

    async Task Destroy()
    {
        if (ConnectionString is not null)
        {
            await using (var cluster = await Database.Cluster())
            {
                var databases = await cluster.DbContext.DatabaseNames.Where(row => row.Kind != DatabaseKind.Cluster).Select(row => row.PhysicalName).ToArrayAsync();
                foreach (var name in databases)
                {
                    await fixture.DropDatabase(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString);
                }
            }

            await fixture.DropDatabase(ConnectionString);
        }

        await Services.DisposeAsync();
    }
}
