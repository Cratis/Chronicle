// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Compares the model's required columns and primary key with catalog metadata without executing DDL.
/// </summary>
internal static class DynamicTableShape
{
    /// <summary>
    /// Reports every mismatch for a modeled dynamic table.
    /// </summary>
    /// <param name="context">The modeled table context.</param>
    /// <param name="table">The migration's expected table definition.</param>
    /// <returns>All missing tables, missing or incompatible columns, and primary-key mismatches.</returns>
    internal static async Task<IReadOnlyList<string>> Differences(DbContext context, CreateTableOperation table)
    {
        if (!(await SchemaTables.Read(context)).ContainsKey(table.Name))
        {
            return [$"table '{table.Name}' is missing"];
        }

        var columns = await Columns(context, table.Name);
        var actual = columns.ToDictionary(column => column.Name, StringComparer.Ordinal);
        var differences = new List<string>();
        foreach (var expected in table.Columns)
        {
            if (!actual.TryGetValue(expected.Name, out var found))
            {
                differences.Add($"column '{table.Name}.{expected.Name}' is missing");
            }
            else if (!Compatible(expected.ColumnType ?? context.GetService<IRelationalTypeMappingSource>().FindMapping(expected.ClrType)!.StoreType, found.StoreType) || (found.Nullable && !expected.IsNullable))
            {
                differences.Add($"column '{table.Name}.{expected.Name}' expected {expected.ColumnType} nullable={expected.IsNullable}, found {found.StoreType} nullable={found.Nullable}");
            }
        }
        var expectedKeys = table.PrimaryKey?.Columns ?? [];
        var actualKeys = columns.Where(column => column.KeyOrdinal > 0).OrderBy(column => column.KeyOrdinal).Select(column => column.Name);
        if (!expectedKeys.SequenceEqual(actualKeys, StringComparer.Ordinal))
        {
            differences.Add($"table '{table.Name}' has a different primary key");
        }
        return differences;
    }

    /// <summary>
    /// Canonicalizes provider type aliases without discarding length, precision, or scale.
    /// </summary>
    /// <param name="value">The migration or catalog store type.</param>
    /// <returns>The comparable store type.</returns>
    internal static string Normalize(string value) => value.ToLowerInvariant().Replace("character varying", "varchar", StringComparison.Ordinal)
        .Replace("character(", "char(", StringComparison.Ordinal)
        .Replace("timestamp with time zone", "timestamptz", StringComparison.Ordinal)
        .Replace(" without time zone", string.Empty, StringComparison.Ordinal)
        .Replace(" ", string.Empty, StringComparison.Ordinal) switch
    {
        "int" or "int4" => "integer",
        "int2" => "smallint",
        "int8" => "bigint",
        "bool" => "boolean",
        "float4" => "real",
        "float8" => "doubleprecision",
        "decimal" => "numeric",
        var type when type.StartsWith("decimal(", StringComparison.Ordinal) => "numeric" + type[7..],
        var type => type
    };

    static async Task<List<Column>> Columns(DbContext context, string table)
    {
        var type = context.Database.GetDatabaseType();
        if (type == DatabaseType.Sqlite)
        {
            var rows = await context.Database.SqlQuery<SqliteColumn>($"SELECT name AS Name, type AS Type, \"notnull\" AS \"Notnull\", pk AS Pk FROM pragma_table_info({table})").ToListAsync();
            return rows.ConvertAll(row => new Column { Name = row.Name, StoreType = row.Type, Nullable = row.Notnull == 0 && row.Pk == 0, KeyOrdinal = row.Pk });
        }
        var sql = type == DatabaseType.PostgreSql
            ? "SELECT a.attname AS \"Name\", pg_catalog.format_type(a.atttypid,a.atttypmod) AS \"StoreType\", NOT a.attnotnull AS \"Nullable\", " +
              "COALESCE(array_position(i.indkey::smallint[], a.attnum),-1)+1 AS \"KeyOrdinal\" " +
              "FROM pg_class t JOIN pg_namespace n ON n.oid=t.relnamespace JOIN pg_attribute a ON a.attrelid=t.oid " +
              "LEFT JOIN pg_index i ON i.indrelid=t.oid AND i.indisprimary " +
              "WHERE t.relname={0} AND n.nspname=current_schema() AND a.attnum>0 AND NOT a.attisdropped"
            : "SELECT c.name AS Name, TYPE_NAME(c.user_type_id) + CASE " +
              "WHEN TYPE_NAME(c.user_type_id) IN ('nvarchar','nchar') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CAST(c.max_length/2 AS varchar) END + ')' " +
              "WHEN TYPE_NAME(c.user_type_id) IN ('varchar','char','varbinary') THEN '(' + CASE WHEN c.max_length=-1 THEN 'max' ELSE CAST(c.max_length AS varchar) END + ')' " +
              "WHEN TYPE_NAME(c.user_type_id) IN ('decimal','numeric') THEN '(' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')' ELSE '' END AS StoreType, " +
              "c.is_nullable AS Nullable, COALESCE(ic.key_ordinal,0) AS KeyOrdinal FROM sys.columns c " +
              "LEFT JOIN sys.indexes i ON i.object_id=c.object_id AND i.is_primary_key=1 " +
              "LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.column_id=c.column_id " +
              "WHERE c.object_id=OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0}))";
        return await context.Database.SqlQueryRaw<Column>(sql, table).ToListAsync();
    }

    static bool Compatible(string expected, string actual) =>
        string.Equals(Normalize(expected), Normalize(actual), StringComparison.Ordinal);

    private sealed class Column
    {
        public string Name { get; set; } = string.Empty;
        public string StoreType { get; set; } = string.Empty;
        public bool Nullable { get; set; }
        public int KeyOrdinal { get; set; }
    }

    private sealed class SqliteColumn
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Notnull { get; set; }
        public int Pk { get; set; }
    }
}
