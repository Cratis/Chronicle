// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
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
    /// <returns>Awaitable task.</returns>
    internal static async Task Ensure(ReadModelDbContext context, string table, string column)
    {
        var database = context.Database;
        var databaseType = database.GetDatabaseType();
        var name = ReadModelIndexNames.For(databaseType, table, column);
        var helper = context.GetService<ISqlGenerationHelper>();
        var create = $"CREATE INDEX {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)} ({helper.DelimitIdentifier(column)})";
        if (databaseType == DatabaseType.SqlServer)
        {
            var exists = await database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.indexes WHERE object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name = {name}").AnyAsync();
            if (exists)
            {
                return;
            }
        }
        else
        {
            if (databaseType == DatabaseType.PostgreSql)
            {
                await DropIfInvalid(context, name);
            }

            create = $"CREATE INDEX IF NOT EXISTS {helper.DelimitIdentifier(name)} ON {helper.DelimitIdentifier(table)} ({helper.DelimitIdentifier(column)})";
        }

        await Execute(context, create);
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
            DatabaseType.PostgreSql => database.SqlQuery<StoredIndex>($"SELECT i.relname AS \"Name\", a.attname AS \"Column\" FROM pg_index x JOIN pg_class t ON t.oid = x.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.indkey[0] WHERE t.relname = {storedTable} AND n.nspname = current_schema() AND x.indnatts = 1 AND NOT x.indisprimary AND left(i.relname, 14) = {ReadModelIndexNames.Prefix}"),
            DatabaseType.SqlServer => database.SqlQuery<StoredIndex>($"SELECT i.name AS [Name], c.name AS [Column] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({storedTable})) AND ic.key_ordinal = 1 AND i.is_primary_key = 0 AND LEFT(i.name, 14) = {ReadModelIndexNames.Prefix}"),
            _ => database.SqlQuery<StoredIndex>($"SELECT i.name AS Name, c.name AS \"Column\" FROM pragma_index_list({storedTable}) i JOIN pragma_index_info(i.name) c WHERE substr(i.name, 1, 14) = {ReadModelIndexNames.Prefix}")
        };
        foreach (var index in await indexes.ToListAsync())
        {
            var expected = ReadModelIndexNames.For(databaseType, table, index.Column);
            if (index.Name != expected)
            {
                await Rename(context, table, index, expected);
            }
        }
    }

    static async Task Rename(ReadModelDbContext context, string table, StoredIndex index, string expected)
    {
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
                await Ensure(context, table, index.Column);
                break;
        }
    }

    /// <summary>
    /// A failed or interrupted PostgreSQL index build leaves an invalid index behind that CREATE INDEX IF NOT EXISTS would keep.
    /// </summary>
    /// <param name="context">The read model context.</param>
    /// <param name="name">The index name.</param>
    /// <returns>Awaitable task.</returns>
    static async Task DropIfInvalid(ReadModelDbContext context, string name)
    {
        var invalid = await context.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid = x.indexrelid JOIN pg_namespace n ON n.oid = i.relnamespace WHERE i.relname = {name} AND n.nspname = current_schema() AND NOT x.indisvalid").AnyAsync();
        if (invalid)
        {
            var helper = context.GetService<ISqlGenerationHelper>();
            await Execute(context, $"DROP INDEX IF EXISTS {helper.DelimitIdentifier(name)}");
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
    }
}
