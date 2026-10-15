// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_postgresql_database(PostgreSqlFixture fixture) : Specification
{
    internal string ConnectionString;
    internal Database Database;
    internal ServiceProvider Services;
    internal readonly Cratis.Orleans.Storage.IJobsStorage JobsStorage = Substitute.For<Cratis.Orleans.Storage.IJobsStorage>();
    internal readonly List<string> AdditionalDatabases = [];
    internal string Store = "a-store";
    internal string Namespace = "a-namespace";

    async Task Establish()
    {
        ConnectionString = await fixture.CreateDatabase();
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance);
        Services = new ServiceCollection().AddSingleton<IReadModelMigrator>(migrator).AddSingleton(JobsStorage).BuildServiceProvider();
        Database = CreateDatabase();
    }

    internal Database CreateDatabase() => CreateDatabase(new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString }, Services);

    internal Database CreateDatabase(Configuration.Storage storage, IServiceProvider services) => new(
        services,
        Options.Create(new ChronicleOptions { Storage = storage }),
        new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
        new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
        services.GetRequiredService<IReadModelMigrator>());

    internal string LogicalStoreName => new NpgsqlConnectionStringBuilder(ConnectionString).Database + "+es+" + Store;

    internal async Task SeedStore(string name)
    {
        await using var cluster = await Database.Cluster();
        cluster.DbContext.EventStores.Add(new EventStore { Name = name });
        await cluster.DbContext.SaveChangesAsync();
    }

    internal async Task CreateLegacyDatabase(string name)
    {
        AdditionalDatabases.Add(name);
        await Execute(ConnectionString, $"CREATE DATABASE \"{name}\"");
    }

    internal async Task<string> Binding(DatabaseKind kind, string store, string ns)
    {
        await using var cluster = await Database.Cluster();
        return await cluster.DbContext.DatabaseNames.Where(row => row.Kind == kind && row.EventStore == store && row.Namespace == ns).Select(row => row.PhysicalName).SingleAsync();
    }

    internal string ConnectionFor(string name) => new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;

    internal static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Only specification-owned identifiers and constant fixture SQL are accepted.
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    async Task Destroy()
    {
        if (ConnectionString is not null)
        {
            await using (var cluster = await Database.Cluster())
            {
                AdditionalDatabases.AddRange(await cluster.DbContext.DatabaseNames.Where(row => row.Kind != DatabaseKind.Cluster).Select(row => row.PhysicalName).ToArrayAsync());
            }

            foreach (var name in AdditionalDatabases.Distinct(StringComparer.Ordinal))
            {
                await fixture.DropDatabase(ConnectionFor(name));
            }

            await fixture.DropDatabase(ConnectionString);
        }

        await Services.DisposeAsync();
    }
}
