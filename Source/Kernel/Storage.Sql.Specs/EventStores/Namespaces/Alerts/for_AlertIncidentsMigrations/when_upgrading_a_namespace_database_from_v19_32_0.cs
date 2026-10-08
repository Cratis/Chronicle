// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_AlertIncidentsMigrations;

public class when_upgrading_a_namespace_database_from_v19_32_0 : given.an_unmigrated_namespace_database
{
    const string OpenIndex = "IX_AlertIncidents_Open_Store_Namespace";
    const string Previous = $"NS-{WellKnownTableNames.AlertIncidents}-{nameof(v19_32_0)}";
    bool _hadIndexBefore;
    bool _hasIndexAfter;
    bool _keptExistingIndexes;
    bool _keptRows;

    async Task Establish()
    {
        await using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        // Namespace migration IDs are not EF's timestamp IDs, so construct the prior schema and history
        // directly, as the existing namespace upgrade specs do.
        foreach (var (id, type) in assembly.Migrations.Where(entry => entry.Key != $"NS-{WellKnownTableNames.AlertIncidents}-{nameof(v19_33_0)}"))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }
        await context.Database.ExecuteSqlRawAsync($"INSERT INTO \"{WellKnownTableNames.AlertIncidents}\" (Id, EventStore, Namespace, ObserverId, EventSequenceId, Partition, Condition, Severity, LastChangedAt, LastTransitionSequenceNumber, IsOpen) VALUES ('1', 's', 'n', 'o', 'e', 'p', 'c', 1, 't', 1, 1)");
        _hadIndexBefore = await IndexExists(context, OpenIndex);
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _hasIndexAfter = await IndexExists(context, OpenIndex);
        _keptExistingIndexes = await IndexExists(context, "IX_AlertIncidents_Store_Open_Raised_Id") && await IndexExists(context, "IX_AlertIncidents_Store_Namespace_Open_Raised_Id");
        _keptRows = await context.AlertIncidents.CountAsync(_ => _.IsOpen) == 1;
    }

    [Fact] void should_not_have_the_index_before_the_upgrade() => _hadIndexBefore.ShouldBeFalse();
    [Fact] void should_add_the_open_state_index() => _hasIndexAfter.ShouldBeTrue();
    [Fact] void should_keep_the_existing_indexes() => _keptExistingIndexes.ShouldBeTrue();
    [Fact] void should_keep_existing_rows() => _keptRows.ShouldBeTrue();
    [Fact] void should_sort_after_the_original_migration() => string.CompareOrdinal($"NS-{WellKnownTableNames.AlertIncidents}-{nameof(v19_33_0)}", Previous).ShouldBeGreaterThan(0);

    static async Task<bool> IndexExists(NamespaceDbContext context, string name)
    {
        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
