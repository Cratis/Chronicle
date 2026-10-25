// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

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
    internal static Task Ensure(ReadModelDbContext context, (string Table, string Column) target, string name, bool online) =>
        Ensure(context, (target.Table, target.Column, name), new Execution(online, null));

    /// <summary>
    /// Ensures an index with execution settings scoped to its DDL statements.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="target">The table, column and index name.</param>
    /// <param name="execution">Online mode and an optional DDL timeout override; zero is unbounded.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Ensure(ReadModelDbContext context, (string Table, string Column, string Name) target, Execution execution) =>
        EnsureIndex(context, (target.Table, target.Name, context.GetService<ISqlGenerationHelper>().DelimitIdentifier(target.Column), false), execution);

    /// <summary>
    /// Ensures a scalar or ordered index with the supplied provider-delimited keys and execution settings.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="target">The table, index name, provider-delimited keys and GIN mode.</param>
    /// <param name="execution">Online mode and an optional DDL timeout override; zero is unbounded.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task EnsureIndex(ReadModelDbContext context, (string Table, string Name, string Keys, bool Gin) target, Execution execution)
    {
        var (table, name, keys, gin) = target;
        var database = context.Database;
        var databaseType = database.GetDatabaseType();
        var helper = context.GetService<ISqlGenerationHelper>();
        var method = gin ? " USING GIN" : string.Empty;
        var create = $"CREATE INDEX {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)}{method} ({keys})";
        var exists = databaseType == DatabaseType.SqlServer && await database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.indexes WHERE object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name = {name}").AnyAsync(execution.CancellationToken);
        if (exists || (databaseType != DatabaseType.SqlServer && DynamicSchemaGuard.PolicyFor(context) != DynamicSchemaPolicy.Allow &&
            await ExistsAndIsValid(context, name, execution.CancellationToken)))
        {
            return;
        }
        if (databaseType != DatabaseType.SqlServer)
        {
            if (databaseType == DatabaseType.PostgreSql)
            {
                await DropIfInvalid(context, (table, name), execution);
            }

            create = $"CREATE INDEX {(execution.Online && databaseType == DatabaseType.PostgreSql ? "CONCURRENTLY " : string.Empty)}IF NOT EXISTS {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)}{method} ({keys})";
        }

        var guard = DynamicSchemaGuard.For(context);
        using var warnings = guard.FlushOnDispose();
        guard.Require(context, "index", $"{table}.{name}");
        if (await TryBuildOnline(context, (create, name), execution))
        {
            return;
        }
        await Execute(context, (create, name, execution));
    }

    /// <summary>
    /// Drops an index, concurrently on PostgreSQL when requested.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="name">The index name.</param>
    /// <param name="online">Whether to request an online drop.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Drop(ReadModelDbContext context, string table, string name, bool online) =>
        Drop(context, (table, name), new Execution(online, null));

    /// <summary>
    /// Drops an index with execution settings scoped to its DDL statement.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="target">The table and index name.</param>
    /// <param name="execution">Online mode and an optional DDL timeout override; zero is unbounded.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Drop(ReadModelDbContext context, (string Table, string Name) target, Execution execution)
    {
        var (table, name) = target;
        var helper = context.GetService<ISqlGenerationHelper>();
        var drop = context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => $"DROP INDEX {(execution.Online ? "CONCURRENTLY " : string.Empty)}IF EXISTS {helper.DelimitIdentifier(name)}",
            DatabaseType.SqlServer => $"DROP INDEX {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)}",
            _ => $"DROP INDEX IF EXISTS {helper.DelimitIdentifier(name)}"
        };
        return DropGuarded(context, table, name, drop, execution);
    }

    static async Task DropGuarded(ReadModelDbContext context, string table, string name, string sql, Execution execution)
    {
        var guard = DynamicSchemaGuard.For(context);
        using var warnings = guard.FlushOnDispose();
        guard.Require(context, "index", $"{table}.{name}");
        await Execute(context, (sql, name, execution));
    }

    static Task<bool> ExistsAndIsValid(ReadModelDbContext context, string name, CancellationToken cancellationToken) =>
        context.Database.IsNpgsql()
            ? context.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid JOIN pg_namespace n ON n.oid=i.relnamespace WHERE i.relname={name} AND n.nspname=current_schema() AND x.indisvalid").AnyAsync(cancellationToken)
            : context.Database.SqlQuery<int>($"SELECT 1 AS Value FROM sqlite_master WHERE type='index' AND name={name}").AnyAsync(cancellationToken);

    static async Task<bool> TryBuildOnline(ReadModelDbContext context, (string Sql, string Name) create, Execution execution)
    {
        if (!execution.Online || context.Database.GetDatabaseType() != DatabaseType.SqlServer)
        {
            return false;
        }
        var edition = await context.Database.SqlQuery<int>($"SELECT CAST(SERVERPROPERTY('EngineEdition') AS int) AS [Value]").SingleAsync(execution.CancellationToken);
        if (edition is 3 or 5 or 8)
        {
            try
            {
                await Execute(context, (create.Sql + " WITH (ONLINE = ON)", create.Name, execution));
                return true;
            }
            catch (SqlException exception) when (exception.Number == 1712)
            {
                // The edition supports online DDL, but this index or server configuration does not.
            }
        }
        return false;
    }

    static async Task DropIfInvalid(ReadModelDbContext context, (string Table, string Name) target, Execution execution)
    {
        var name = target.Name;
        var invalid = await context.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_namespace n ON n.oid = i.relnamespace WHERE i.relname = {name} AND n.nspname = current_schema() AND NOT x.indisvalid").AnyAsync(execution.CancellationToken);
        if (invalid)
        {
            await Drop(context, target, execution);
        }
    }

    static async Task<int> Execute(ReadModelDbContext context, (string Sql, string Name, Execution Execution) command)
    {
        var (sql, name, execution) = command;
        var commandTimeout = execution.CommandTimeout;
        var timeout = context.Database.GetCommandTimeout();
        if (commandTimeout.HasValue)
        {
            context.Database.SetCommandTimeout(commandTimeout);
        }
        try
        {
#pragma warning disable EF1002 // Identifiers are delimited by ISqlGenerationHelper.
            return await context.Database.ExecuteSqlRawAsync(sql, execution.CancellationToken);
#pragma warning restore EF1002
        }
        catch (Exception exception) when (exception is not SqlException { Number: 1712 })
        {
            execution.Logger?.ShadowIndexDdlFailed(context.TableName, name, exception);
            throw;
        }
        finally
        {
            context.Database.SetCommandTimeout(timeout);
        }
    }

    /// <summary>
    /// Execution settings for index operations; timeout overrides apply only to DDL.
    /// </summary>
    /// <param name="Online">Whether to request online execution.</param>
    /// <param name="CommandTimeout">An optional DDL timeout override; zero is unbounded.</param>
    /// <param name="Logger">The migration logger for shadow-index DDL failures.</param>
    /// <param name="CancellationToken">The caller's cancellation token or reconciliation deadline.</param>
    internal readonly record struct Execution(bool Online, int? CommandTimeout, ILogger<ReadModelMigrator>? Logger = null, CancellationToken CancellationToken = default);
}
