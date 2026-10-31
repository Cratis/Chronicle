// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations.for_RecommendationsMigrations;

public class when_upgrading_from_v15_0_0 : given.an_unmigrated_namespace_database
{
    const string Previous = $"NS-{WellKnownTableNames.Recommendations}-{nameof(v15_0_0)}";
    const string Current = $"NS-{WellKnownTableNames.Recommendations}-{nameof(v19_115_0_2)}";
    bool _hadColumnBefore;
    bool _hasColumnAfter;
    bool _keptRowsActive;

    async Task Establish()
    {
        await using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        foreach (var (id, type) in assembly.Migrations.Where(entry => entry.Key != Current))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            var targetModel = context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel, designTime: true);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations, targetModel))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }
        await context.Database.ExecuteSqlRawAsync($"INSERT INTO \"{WellKnownTableNames.Recommendations}\" (Id, Name, Description, Type, Occurred, RequestJson) VALUES ('1', 'n', 'd', 't', '2026-01-01', 'null')");
        _hadColumnBefore = await ColumnExists(context);
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _hasColumnAfter = await ColumnExists(context);
        _keptRowsActive = await context.Recommendations.CountAsync(_ => !_.IsIgnored) == 1;
    }

    [Fact] void should_not_have_the_column_before_the_upgrade() => _hadColumnBefore.ShouldBeFalse();
    [Fact] void should_add_the_ignored_column() => _hasColumnAfter.ShouldBeTrue();
    [Fact] void should_keep_existing_rows_active() => _keptRowsActive.ShouldBeTrue();
    [Fact] void should_sort_after_the_original_migration() => string.CompareOrdinal(Current, Previous).ShouldBeGreaterThan(0);

    static async Task<bool> ColumnExists(NamespaceDbContext context)
    {
        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{WellKnownTableNames.Recommendations}') WHERE name = 'IsIgnored'";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
