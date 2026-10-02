// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

public abstract class an_accumulating_sql_read_model<THarness> : Contract.an_accumulating_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected THarness _sqlHarness;

    protected sealed override THarness CreateHarness() => _sqlHarness = CreateSqlHarness();

    protected virtual THarness CreateSqlHarness() => new();

    protected ReadModelDbContext OpenInspectionContext(string? connectionString = null)
    {
        var definition = CreateReadModelDefinition();
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(connectionString ?? _sqlHarness.ConnectionString);
        return new ReadModelDbContext(
            builder.AddConceptAsSupport().Options,
            definition.ContainerName.Value,
            ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration()),
            Substitute.For<IReadModelMigrator>());
    }

    protected async Task<int?> StoredCount(string table)
    {
        await using var context = OpenInspectionContext();
        var helper = context.GetService<ISqlGenerationHelper>();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Specification-owned identifiers delimited by the provider.
        command.CommandText = $"SELECT {helper.DelimitIdentifier("count")} FROM {helper.DelimitIdentifier(table)}";
#pragma warning restore CA2100
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    protected async Task<bool> TableExists(string table)
    {
        // Inspect the catalog without ReadModelTable/EnsureTableExists recreating the table under test.
        await using var context = OpenInspectionContext();
        var database = context.Database;
        var databaseType = database.GetDatabaseType();
        var name = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var query = databaseType switch
        {
            DatabaseType.PostgreSql => database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_tables WHERE schemaname = current_schema() AND tablename = {name}"),
            DatabaseType.SqlServer => database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.tables WHERE schema_id = SCHEMA_ID() AND name = {name}"),
            _ => database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = {name}")
        };
        return await query.AnyAsync();
    }
}
