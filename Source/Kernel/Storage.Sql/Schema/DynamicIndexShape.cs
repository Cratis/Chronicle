// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Reads the shape of the unique Value index protecting a dynamic constraint table.
/// </summary>
internal static class DynamicIndexShape
{
    /// <summary>
    /// Checks that an index is unique and has only the Value key.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="table">The dynamic constraint table.</param>
    /// <param name="index">The required index name.</param>
    /// <returns>Whether the catalog contains the required shape.</returns>
    internal static async Task<bool> IsUniqueValue(DbContext context, string table, string index)
    {
        var type = context.Database.GetDatabaseType();
        if (type == DatabaseType.Sqlite)
        {
            var indexes = await context.Database.SqlQuery<SqliteIndex>($"SELECT name AS Name, \"unique\" AS \"Unique\", partial AS Partial FROM pragma_index_list({table})").ToListAsync();
            if (!indexes.Exists(item => item.Name == index && item.Unique == 1 && item.Partial == 0)) return false;
            var keys = await context.Database.SqlQuery<SqliteKey>($"SELECT name AS Name FROM pragma_index_info({index}) ORDER BY seqno").ToListAsync();
            return keys.Count == 1 && keys[0].Name == "Value";
        }
        var sql = type == DatabaseType.PostgreSql
            ? "SELECT a.attname AS \"Value\" FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace " +
              "JOIN pg_index ix ON ix.indrelid=t.oid JOIN pg_class i ON i.oid=ix.indexrelid " +
              "JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=ANY(ix.indkey) " +
              "WHERE t.relname={0} AND i.relname={1} AND n.nspname=current_schema() AND ix.indisunique AND ix.indisvalid " +
              "AND ix.indisready AND ix.indpred IS NULL AND ix.indnkeyatts=1 AND ix.indnatts=1"
            : "SELECT c.name AS [Value] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id " +
              "JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id " +
              "WHERE i.object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND i.name={1} AND i.is_unique=1 AND i.is_disabled=0 AND i.has_filter=0 AND ic.key_ordinal>0";
        var columns = await context.Database.SqlQueryRaw<string>(sql, table, index).ToListAsync();
        return columns.Count == 1 && columns[0] == "Value";
    }

    /// <summary>
    /// Checks that a named, usable index exists on a table.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="table">The table.</param>
    /// <param name="index">The index name.</param>
    /// <returns>Whether the index exists and is valid.</returns>
    internal static Task<bool> Exists(DbContext context, string table, string index) =>
        context.Database.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => context.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_index x JOIN pg_class t ON t.oid=x.indrelid JOIN pg_class i ON i.oid=x.indexrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE t.relname={table} AND i.relname={index} AND n.nspname=current_schema() AND x.indisvalid AND x.indisready").AnyAsync(),
            DatabaseType.SqlServer => context.Database.SqlQuery<int>($"SELECT 1 AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({table})) AND name={index} AND is_disabled=0").AnyAsync(),
            _ => context.Database.SqlQuery<int>($"SELECT 1 AS Value FROM pragma_index_list({table}) WHERE name={index}").AnyAsync()
        };

    /// <summary>
    /// Compares a migration index's uniqueness and ordered column keys with the catalog.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="index">The expected migration index.</param>
    /// <returns>The inspection task.</returns>
    internal static async Task Verify(DbContext context, CreateIndexOperation index)
    {
        var type = context.Database.GetDatabaseType();
        string[] columns;
        if (type == DatabaseType.Sqlite)
        {
            var indexes = await context.Database.SqlQuery<SqliteIndex>($"SELECT name AS Name, \"unique\" AS \"Unique\", partial AS Partial FROM pragma_index_list({index.Table})").ToListAsync();
            if (!indexes.Exists(item => item.Name == index.Name && item.Unique == (index.IsUnique ? 1 : 0) && item.Partial == (index.Filter is null ? 0 : 1)))
            {
                DynamicSchemaVerification.Report($"index '{index.Table}.{index.Name}' is missing or has different uniqueness or filtering");
                return;
            }
            var keys = await context.Database.SqlQuery<SqliteKey>($"SELECT name AS Name FROM pragma_index_info({index.Name}) ORDER BY seqno").ToListAsync();
            columns = keys.Select(key => key.Name).ToArray();
        }
        else
        {
            var sql = type == DatabaseType.PostgreSql
                ? "SELECT a.attname AS \"Value\" FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace " +
                  "JOIN pg_index ix ON ix.indrelid=t.oid JOIN pg_class i ON i.oid=ix.indexrelid " +
                  "JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS k(attnum,position) ON true JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=k.attnum " +
                  "WHERE t.relname={0} AND i.relname={1} AND n.nspname=current_schema() AND ix.indisunique={2} AND ix.indisvalid AND ix.indisready AND (ix.indpred IS NOT NULL)={3} ORDER BY k.position"
                : "SELECT c.name AS [Value] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id " +
                  "JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id " +
                  "WHERE i.object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND i.name={1} AND i.is_unique={2} AND i.is_disabled=0 AND i.has_filter={3} AND ic.key_ordinal>0 ORDER BY ic.key_ordinal";
            columns = (await context.Database.SqlQueryRaw<string>(sql, index.Table, index.Name, index.IsUnique, index.Filter is not null).ToListAsync()).ToArray();
        }
        if (!columns.SequenceEqual(index.Columns, StringComparer.Ordinal)) DynamicSchemaVerification.Report($"index '{index.Table}.{index.Name}' is missing or has different ordered keys or uniqueness");
        if (index.Filter is not null && !await HasExpectedFilter(context, index)) DynamicSchemaVerification.Report($"index '{index.Table}.{index.Name}' has a different filter");
    }

    static async Task<bool> HasExpectedFilter(DbContext context, CreateIndexOperation index)
    {
        var type = context.Database.GetDatabaseType();
        var sql = type switch
        {
            DatabaseType.PostgreSql => "SELECT pg_get_expr(ix.indpred,ix.indrelid) AS \"Value\" FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace " +
                "JOIN pg_index ix ON ix.indrelid=t.oid JOIN pg_class i ON i.oid=ix.indexrelid WHERE t.relname={0} AND i.relname={1} AND n.nspname=current_schema() AND ix.indpred IS NOT NULL",
            DatabaseType.SqlServer => "SELECT filter_definition AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND name={1} AND has_filter=1",
            _ => "SELECT substr(sql,instr(upper(sql),' WHERE ')+7) AS Value FROM sqlite_master WHERE type='index' AND tbl_name={0} AND name={1} AND instr(upper(sql),' WHERE ')>0"
        };
        var filters = await context.Database.SqlQueryRaw<string>(sql, index.Table, index.Name).ToListAsync();
        return filters.Count == 1 && string.Equals(Unwrap(filters[0]), Unwrap(index.Filter!), StringComparison.Ordinal);
    }

    static string Unwrap(string filter)
    {
        filter = filter.Trim();
        while (filter.StartsWith('(') && filter.EndsWith(')'))
        {
            var depth = 0;
            var wraps = true;
            for (var i = 0; i < filter.Length - 1; i++)
            {
                if (filter[i] == '(') depth++;
                if (filter[i] == ')') depth--;
                if (depth == 0) wraps = false;
            }
            if (!wraps) break;
            filter = filter[1..^1].Trim();
        }
        return filter;
    }

    private sealed class SqliteIndex
    {
        public string Name { get; set; } = string.Empty;
        public int Unique { get; set; }
        public int Partial { get; set; }
    }

    private sealed class SqliteKey
    {
        public string Name { get; set; } = string.Empty;
    }
}
