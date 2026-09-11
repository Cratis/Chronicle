// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_retrying_after_head_reuse : ArchiveRetryConformance
{
    SqliteConnection _connection;
    IDatabase _database;
    string? _externalConnection;
    string? _provider;

    void Establish()
    {
        _provider = Environment.GetEnvironmentVariable("CHRONICLE_REGISTRY_SQL_PROVIDER");
        var connection = Environment.GetEnvironmentVariable("CHRONICLE_REGISTRY_SQL_CONNECTION");
        _externalConnection = connection is null ? null : $"{connection};Database=registry_{Guid.NewGuid():N}";
        _connection = new("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();

        // Exercise the actual provider migration, not just EF's current-model schema creation.
        var migration = new registry_migration();
        ApplyMigration(context, migration.Remove(context.Database.ProviderName!));
        ApplyMigration(context, migration.Create(context.Database.ProviderName!));
        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
    }

    protected override IEventSequenceMutationRegistry RecreateRegistry() => new EventSequenceMutationRegistry("store", "namespace", _database);

    NamespaceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>();
        switch (_provider)
        {
            case "PostgreSQL":
                options.UseNpgsql(_externalConnection);
                break;
            case "SqlServer":
                options.UseSqlServer(_externalConnection);
                break;
            default:
                options.UseSqlite(_connection);
                break;
        }

        return new(options.AddConceptAsSupport().Options);
    }

    static void ApplyMigration(NamespaceDbContext context, MigrationBuilder builder)
    {
        var generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (var command in generator.Generate(builder.Operations, context.Model))
        {
            context.Database.ExecuteSqlRaw(command.CommandText);
        }
    }

    void Destroy()
    {
        if (_externalConnection is not null)
        {
            using var context = CreateContext();
            context.Database.EnsureDeleted();
        }

        _connection.Dispose();
    }

    sealed class registry_migration : Migrations.v16_45_0
    {
        internal MigrationBuilder Remove(string provider)
        {
            var builder = new MigrationBuilder(provider);
            Down(builder);
            return builder;
        }

        internal MigrationBuilder Create(string provider)
        {
            var builder = new MigrationBuilder(provider);
            Up(builder);
            return builder;
        }
    }
}
