// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Creates scalar read-model indexes and keeps their names aligned with replay table renames.
/// Callers hold the database schema lock.
/// </summary>
internal static class ReadModelIndexes
{
    /// <summary>
    /// Ensure an index exists for a column.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="column">The column name.</param>
    /// <param name="online">Whether an online build is required.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Ensure(ReadModelDbContext context, string table, string column, bool online = false) =>
        EnsureIndex(context, table, ReadModelIndexNames.For(context.Database.GetDatabaseType(), table, column), context.GetService<ISqlGenerationHelper>().DelimitIdentifier(column), false, online);

    /// <summary>
    /// Ensures a complete ordered index exists.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="plan">The complete provider index plan.</param>
    /// <param name="online">Whether an online build is required.</param>
    /// <returns>An awaitable task.</returns>
    internal static Task Ensure(ReadModelDbContext context, string table, ReadModelIndexPlan plan, bool online) =>
        Ensure(context, table, plan, new ReadModelIndexOperations.Execution(online, null));

    /// <summary>
    /// Ensures a declared index using the supplied DDL execution settings.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="plan">The resolved index plan.</param>
    /// <param name="execution">The DDL timeout and cancellation settings.</param>
    /// <returns>The index preparation task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, string table, ReadModelIndexPlan plan, ReadModelIndexOperations.Execution execution)
    {
        var type = context.Database.GetDatabaseType();
        if (type == DatabaseType.SqlServer) await SqlServerIndexColumns.Ensure(context, table, plan.Targets);
        await ReadModelIndexOperations.EnsureIndex(context, (table, plan.Name(type, table), plan.Keys(type, context.GetService<ISqlGenerationHelper>()), plan.IsGin), execution);
    }

    /// <summary>
    /// Rename the indexes of a table so they follow its current name.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="table">The table name.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task NameAfterTable(ReadModelDbContext context, string table)
    {
        var database = context.Database;
        var databaseType = database.GetDatabaseType();
        var storedTable = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var indexes = databaseType switch
        {
            DatabaseType.PostgreSql => database.SqlQuery<StoredIndex>($"SELECT i.relname AS \"Name\", COALESCE(a.attname, '') AS \"Column\", '' AS \"Definition\" FROM pg_index x JOIN pg_class t ON t.oid = x.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace JOIN pg_class i ON i.oid = x.indexrelid LEFT JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.indkey[0] WHERE t.relname = {storedTable} AND n.nspname = current_schema() AND NOT x.indisprimary AND left(i.relname, 14) = {ReadModelIndexNames.Prefix}"),
            DatabaseType.SqlServer => database.SqlQuery<StoredIndex>($"SELECT i.name AS [Name], c.name AS [Column], '' AS [Definition] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({storedTable})) AND ic.key_ordinal = 1 AND i.is_primary_key = 0 AND LEFT(i.name, 14) = {ReadModelIndexNames.Prefix}"),
            _ => database.SqlQuery<StoredIndex>($"SELECT i.name AS Name, COALESCE(c.name, '') AS \"Column\", s.sql AS Definition FROM pragma_index_list({storedTable}) i JOIN pragma_index_info(i.name) c ON c.seqno=0 JOIN sqlite_master s ON s.name=i.name WHERE substr(i.name, 1, 14) = {ReadModelIndexNames.Prefix}")
        };
        foreach (var index in await indexes.ToListAsync())
        {
            var expected = ReadModelIndexNames.AfterRename(databaseType, table, index.Name, index.Column);
            if (index.Name != expected)
            {
                await Rename(context, table, index, expected);
            }
        }
    }

    /// <summary>
    /// Drops undeclared Chronicle-owned indexes following a definition change.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="table">The table name.</param>
    /// <param name="namesToKeep">The currently declared index names.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task DropUndeclared(ReadModelDbContext context, string table, IEnumerable<string> namesToKeep)
    {
        var database = context.Database;
        var type = database.GetDatabaseType();
        var expected = namesToKeep.ToHashSet(StringComparer.Ordinal);
        var storedTable = PrimaryKeyNames.TableIdentifier(type, table);
        var names = type switch
        {
            DatabaseType.PostgreSql => await database.SqlQuery<string>($"SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={storedTable}").ToListAsync(),
            DatabaseType.SqlServer => await database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({storedTable})) AND name IS NOT NULL").ToListAsync(),
            _ => await database.SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={storedTable}").ToListAsync()
        };
        foreach (var name in names.Where(name => name.StartsWith(ReadModelIndexNames.Prefix, StringComparison.Ordinal) && !expected.Contains(name)))
        {
            await ReadModelIndexOperations.Drop(context, table, name, online: true);
        }
        if (type == DatabaseType.SqlServer) await SqlServerIndexColumns.DropUnused(context, table);
    }

    static Task EnsureIndex(ReadModelDbContext context, string table, string name, string keys, bool gin, bool online) =>
        ReadModelIndexOperations.EnsureIndex(context, (table, name, keys, gin), new ReadModelIndexOperations.Execution(online, null));

    static async Task Rename(ReadModelDbContext context, string table, StoredIndex index, string expected)
    {
        var guard = DynamicSchemaGuard.For(context);
        using var warnings = guard.FlushOnDispose();
        guard.Require(context, "index", $"{table}.{index.Name}");
        var helper = context.GetService<ISqlGenerationHelper>();
        switch (context.Database.GetDatabaseType())
        {
            case DatabaseType.PostgreSql:
                await Execute(context, $"ALTER INDEX {helper.DelimitIdentifier(index.Name)} RENAME TO {helper.DelimitIdentifier(expected)}");
                break;
            case DatabaseType.SqlServer:
                await context.Database.ExecuteSqlAsync($"DECLARE @objname nvarchar(776) = QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table}) + N'.' + QUOTENAME({index.Name}); EXEC sp_rename @objname = @objname, @newname = {expected}, @objtype = N'INDEX';");
                break;
            default:
                // SQLite cannot rename an index. Rebuild it within the table promotion transaction.
                await Execute(context, $"DROP INDEX {helper.DelimitIdentifier(index.Name)}");
                var quotedName = helper.DelimitIdentifier(index.Name);
                var position = index.Definition.IndexOf(quotedName, StringComparison.Ordinal);
                if (position < 0) throw new InvalidOperationException($"Cannot locate managed index name in its definition: {index.Name}");
                await Execute(context, index.Definition.Remove(position, quotedName.Length).Insert(position, helper.DelimitIdentifier(expected)));
                break;
        }
    }

    static Task<int> Execute(ReadModelDbContext context, string sql)
    {
#pragma warning disable EF1002 // Identifiers are delimited by ISqlGenerationHelper.
        return context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }

    sealed class StoredIndex
    {
        public string Name { get; set; } = string.Empty;
        public string Column { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
    }
}
