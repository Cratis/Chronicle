// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.Migrations.for_v19_117_0_3;

public class when_upgrading_an_existing_pre_train6_event_store : Specification
{
    const string MigrationId = $"ObserverRegistrations-{nameof(v19_117_0_3)}";
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    SqliteConnection _connection;
    string _connectionString;
    string[] _pendingBefore;
    string[] _appliedAfter;
    bool _hadTableBefore;
    bool _hasTableAfter;
    bool _keptReactor;
    bool _keptInitialization;

    async Task Establish()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"registration-upgrade-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ConnectionString;
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
        await using var context = CreateContext();
        await CreatePreTrain6Schema(context);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO Reactors (Id, Owner, EventSequenceId, EventTypes, IsReplayable, Tags, Filters) VALUES ('existing-reactor', 1, 'event-log', '[]', 1, 'existing-tags', NULL)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO EventStoreInitialization (Id) VALUES (1)");
        _pendingBefore = (await context.Database.GetPendingMigrationsAsync()).ToArray();
        _hadTableBefore = await Count(context, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ObserverRegistrations'") == 1;
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _appliedAfter = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        _hasTableAfter = await Count(context, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'ObserverRegistrations'") == 1;
        _keptReactor = await Count(context, "SELECT COUNT(*) FROM Reactors WHERE Id = 'existing-reactor' AND Owner = 1 AND EventSequenceId = 'event-log' AND EventTypes = '[]' AND IsReplayable = 1 AND Tags = 'existing-tags' AND Filters IS NULL") == 1;
        _keptInitialization = await Count(context, "SELECT COUNT(*) FROM EventStoreInitialization WHERE Id = 1") == 1;
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_have_only_the_reserved_migration_pending() => _pendingBefore.ShouldEqual([MigrationId]);
    [Fact] void should_not_have_the_registration_table_before_upgrading() => _hadTableBefore.ShouldBeFalse();
    [Fact] void should_apply_the_reserved_migration() => _appliedAfter.ShouldContain(MigrationId);
    [Fact] void should_create_the_registration_table() => _hasTableAfter.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_reactor_payload() => _keptReactor.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_initialization_marker() => _keptInitialization.ShouldBeTrue();

    static async Task CreatePreTrain6Schema(EventStoreDbContext context)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        // Reconstruct the event-store schema available at the 19.115.3 version ceiling from current
        // sources, not a release-binary snapshot or a namespace database. IDs are table-scoped, not
        // release timestamps: generate each prior migration and its real EF history row, following
        // the existing AlertIncidents upgrade specs. HistoryRow's version is EF's version, not Chronicle's.
        foreach (var (id, type) in assembly.Migrations.Where(entry => Version.Parse(entry.Value.Name[1..].Replace('_', '.')) <= new Version(19, 115, 3)))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            var targetModel = context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel, designTime: true);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations, targetModel))
            {
                // Execute generated SQL verbatim: JSON defaults can contain braces, not format placeholders.
                await command.ExecuteNonQueryAsync(context.GetService<IRelationalConnection>());
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }
    }

    static async Task<long> Count(EventStoreDbContext context, string sql)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Test-only helper: every caller supplies a fixed SQL literal; no external input.
        command.CommandText = sql;
#pragma warning restore CA2100
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    EventStoreDbContext CreateContext() => new(Database.BuildOptions<EventStoreDbContext>(_services, _connectionString));
}
