// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Executes provider-specific index DDL for declared and shadow indexes. Callers hold the schema lock.
/// Online operations must run outside a transaction.
/// </summary>
internal static class ReadModelIndexOperations
{
    /// <summary>
    /// Ensures the named index exists, using the provider's online build when requested and supported.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="target">The table and column to index.</param>
    /// <param name="name">The index name.</param>
    /// <param name="online">Whether to request an online build.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, (string Table, string Column) target, string name, bool online)
    {
        var (table, column) = target;
        var database = context.Database;
        var databaseType = database.GetDatabaseType();
        var helper = context.GetService<ISqlGenerationHelper>();
        var create = $"CREATE INDEX {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)} ({helper.DelimitIdentifier(column)})";
        var exists = databaseType == DatabaseType.SqlServer && await database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.indexes WHERE object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name = {name}").AnyAsync();
        if (exists)
        {
            return;
        }
        if (databaseType != DatabaseType.SqlServer)
        {
            if (databaseType == DatabaseType.PostgreSql)
            {
                await DropIfInvalid(context, name, online);
            }

            create = $"CREATE INDEX {(online && databaseType == DatabaseType.PostgreSql ? "CONCURRENTLY " : string.Empty)}IF NOT EXISTS {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)} ({helper.DelimitIdentifier(column)})";
        }

        if (await TryBuildOnline(context, create, online))
        {
            return;
        }
        await Execute(context, create);
    }

    /// <summary>
    /// Drops an index, concurrently on PostgreSQL when requested.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="name">The index name.</param>
    /// <param name="online">Whether to request an online drop.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Drop(ReadModelDbContext context, string table, string name, bool online)
    {
        var helper = context.GetService<ISqlGenerationHelper>();
        var drop = context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => $"DROP INDEX {(online ? "CONCURRENTLY " : string.Empty)}IF EXISTS {helper.DelimitIdentifier(name)}",
            DatabaseType.SqlServer => $"DROP INDEX {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)}",
            _ => $"DROP INDEX IF EXISTS {helper.DelimitIdentifier(name)}"
        };
        return Execute(context, drop);
    }

    static async Task<bool> TryBuildOnline(ReadModelDbContext context, string create, bool online)
    {
        if (!online || context.Database.GetDatabaseType() != DatabaseType.SqlServer)
        {
            return false;
        }
        var edition = await context.Database.SqlQuery<int>($"SELECT CAST(SERVERPROPERTY('EngineEdition') AS int) AS [Value]").SingleAsync();
        if (edition is 3 or 5 or 8)
        {
            try
            {
                await Execute(context, create + " WITH (ONLINE = ON)");
                return true;
            }
            catch (SqlException exception) when (exception.Number == 1712)
            {
                // The edition supports online DDL, but this index or server configuration does not.
            }
        }
        return false;
    }

    static async Task DropIfInvalid(ReadModelDbContext context, string name, bool online)
    {
        var invalid = await context.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_namespace n ON n.oid = i.relnamespace WHERE i.relname = {name} AND n.nspname = current_schema() AND NOT x.indisvalid").AnyAsync();
        if (invalid)
        {
            var helper = context.GetService<ISqlGenerationHelper>();
            await Execute(context, $"DROP INDEX {(online ? "CONCURRENTLY " : string.Empty)}IF EXISTS {helper.DelimitIdentifier(name)}");
        }
    }

    static Task<int> Execute(ReadModelDbContext context, string sql)
    {
#pragma warning disable EF1002 // Identifiers are delimited by ISqlGenerationHelper.
        return context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }
}
