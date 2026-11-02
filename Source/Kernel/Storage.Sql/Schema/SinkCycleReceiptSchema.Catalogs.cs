// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Schema;

#pragma warning disable EF1002 // The catalog statements are constants of this assembly; the table and index names are parameters.

/// <summary>
/// The read-only catalog inspection of the receipt table.
/// </summary>
internal static partial class SinkCycleReceiptSchema
{
#pragma warning disable MA0136 // Line endings in these statements are only whitespace between clauses.

    /// <summary>
    /// One statement per provider describes the table the unqualified receipt SQL resolves to. Lines are tagged:
    /// C|name|type|notnull|default|generated, P|primary key column, I|index|unique|partial, K|index|column|position.
    /// </summary>
    const string SqliteCatalog = """
        SELECT 'C|' || name || '|' || upper(type) || '|' || "notnull" || '|' || coalesce(dflt_value, '') || '|' || (CASE WHEN hidden <> 0 THEN 1 ELSE 0 END) AS "Value" FROM pragma_table_xinfo({0}, 'main')
        UNION ALL SELECT 'P|' || name AS "Value" FROM pragma_table_xinfo({0}, 'main') WHERE pk > 0
        UNION ALL SELECT 'I|' || name || '|' || "unique" || '|' || partial AS "Value" FROM pragma_index_list({0}, 'main')
        UNION ALL SELECT 'K|' || l.name || '|' || coalesce(x.name, '<expression>') || '|' || x.seqno AS "Value" FROM pragma_index_list({0}, 'main') l, pragma_index_xinfo(l.name, 'main') x WHERE x.key = 1
        """;

    const string PostgreSqlCatalog = """
        SELECT 'C|' || a.attname || '|' || format_type(a.atttypid, a.atttypmod) || '|' || (CASE WHEN a.attnotnull THEN 1 ELSE 0 END) || '|' || coalesce(pg_get_expr(d.adbin, d.adrelid), '') || '|' || (CASE WHEN a.attgenerated::text <> '' OR a.attidentity::text <> '' THEN 1 ELSE 0 END) AS "Value"
          FROM pg_attribute a LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
          WHERE a.attrelid = to_regclass(quote_ident({0}::text)) AND a.attnum > 0 AND NOT a.attisdropped
        UNION ALL SELECT 'P|' || a.attname AS "Value"
          FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
          WHERE i.indrelid = to_regclass(quote_ident({0}::text)) AND i.indisprimary
        UNION ALL SELECT 'I|' || c.relname || '|' || (CASE WHEN i.indisunique THEN 1 ELSE 0 END) || '|' || (CASE WHEN i.indpred IS NOT NULL THEN 1 ELSE 0 END) AS "Value"
          FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
          WHERE i.indrelid = to_regclass(quote_ident({0}::text)) AND NOT i.indisprimary
        UNION ALL SELECT 'K|' || c.relname || '|' || coalesce(a.attname::text, '<expression>') || '|' || (k.ord - 1) AS "Value"
          FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid
          CROSS JOIN LATERAL unnest(i.indkey::int2[]) WITH ORDINALITY AS k(attnum, ord)
          LEFT JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = k.attnum
          WHERE i.indrelid = to_regclass(quote_ident({0}::text)) AND NOT i.indisprimary AND k.ord <= i.indnkeyatts
        """;

    const string SqlServerCatalog = """
        SELECT N'C|' + c.name + N'|' + ty.name + (CASE WHEN ty.name IN (N'nvarchar', N'varchar', N'varbinary') THEN N'(' + (CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(CASE WHEN ty.name = N'nvarchar' THEN c.max_length / 2 ELSE c.max_length END AS nvarchar(10)) END) + N')' ELSE N'' END) + N'|' + CAST(1 - c.is_nullable AS nvarchar(1)) + N'|' + coalesce(d.definition, N'') + N'|' + CAST(CASE WHEN c.is_computed = 1 OR c.is_identity = 1 THEN 1 ELSE 0 END AS nvarchar(1)) AS [Value]
          FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id LEFT JOIN sys.default_constraints d ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
          WHERE c.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0}))
        UNION ALL SELECT N'P|' + c.name AS [Value]
          FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND i.is_primary_key = 1
        UNION ALL SELECT N'I|' + i.name + N'|' + CAST(i.is_unique AS nvarchar(1)) + N'|' + CAST(i.has_filter AS nvarchar(1)) AS [Value]
          FROM sys.indexes i WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND i.is_primary_key = 0 AND i.type > 0
        UNION ALL SELECT N'K|' + i.name + N'|' + c.name + N'|' + CAST(ic.key_ordinal - 1 AS nvarchar(10)) AS [Value]
          FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({0})) AND i.is_primary_key = 0 AND i.type > 0 AND ic.is_included_column = 0 AND ic.key_ordinal > 0
        """;

#pragma warning restore MA0136

    static async Task<ReceiptInspection> Inspect(DbContext context)
    {
        try
        {
            var databaseType = context.Database.GetDatabaseType();
            var sql = databaseType switch
            {
                DatabaseType.PostgreSql => PostgreSqlCatalog,
                DatabaseType.SqlServer => SqlServerCatalog,
                _ => SqliteCatalog
            };
            var lines = await context.Database.SqlQueryRaw<string>(sql, Table).ToListAsync();
            return Compare(databaseType, lines);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not SchemaNotCurrent)
        {
            // Without visibility the receipts cannot be shown to exist, so the check fails closed.
            return new(false, [], [$"receipt table '{Table}' could not be inspected ({exception.GetType().Name}: {exception.Message})"]);
        }
    }

    static ReceiptInspection Compare(DatabaseType databaseType, IReadOnlyList<string> lines)
    {
        var parts = lines.Select(line => line.Split('|')).ToArray();
        var columns = parts.Where(p => p[0] == "C").ToDictionary(p => p[1], StringComparer.Ordinal);
        if (columns.Count == 0)
        {
            return new(true, [$"receipt table '{Table}' is missing"], []);
        }

        var unsafeDrift = new List<string>();
        CompareColumns(databaseType, columns, unsafeDrift);
        CompareKey(parts, unsafeDrift);
        var missingIndex = CompareIndex(parts, unsafeDrift);
        return new(false, missingIndex ? [$"index '{IndexName}' is missing"] : [], unsafeDrift);
    }

    static void CompareColumns(DatabaseType databaseType, Dictionary<string, string[]> columns, List<string> unsafeDrift)
    {
        foreach (var name in _columns)
        {
            if (columns.TryGetValue(name, out var column))
            {
                CompareColumn(databaseType, name, column, unsafeDrift);
            }
            else
            {
                unsafeDrift.Add($"column '{name}' is missing");
            }
        }

        foreach (var column in columns.Values.Where(IsRequiredWithoutDefault))
        {
            unsafeDrift.Add($"column '{column[1]}' is required and has no default");
        }
    }

    static bool IsRequiredWithoutDefault(string[] column) =>
        !_columns.Contains(column[1], StringComparer.Ordinal) && column[3] == "1" && column[4].Length == 0 && column[^1] == "0";

    static void CompareKey(string[][] parts, List<string> unsafeDrift)
    {
        var key = parts.Where(p => p[0] == "P").Select(p => p[1]).ToArray();
        if (key.Length != 1 || key[0] != "Id")
        {
            unsafeDrift.Add($"primary key is ({string.Join(", ", key)}) instead of (Id)");
        }
    }

    static void CompareColumn(DatabaseType databaseType, string name, string[] column, List<string> unsafeDrift)
    {
        var expected = ExpectedType(databaseType, name);
        if (!string.Equals(column[2], expected, StringComparison.OrdinalIgnoreCase))
        {
            unsafeDrift.Add($"column '{name}' has type '{column[2]}' instead of '{expected}'");
        }

        if (column[3] != "1")
        {
            unsafeDrift.Add($"column '{name}' is nullable");
        }

        if (column[^1] != "0")
        {
            unsafeDrift.Add($"column '{name}' is generated");
        }

        if (name == "CreatedAt" && Normalize(column[4]) != Normalize(SinkCycleReceipt.NowSql(databaseType)))
        {
            unsafeDrift.Add($"column '{name}' has default '{column[4]}' instead of the database clock");
        }
    }

    static bool CompareIndex(string[][] parts, List<string> unsafeDrift)
    {
        var index = parts.FirstOrDefault(p => p[0] == "I" && string.Equals(p[1], IndexName, StringComparison.Ordinal));
        if (index is null)
        {
            return true;
        }

        var keys = parts.Where(p => p[0] == "K" && p[1] == IndexName).OrderBy(p => int.Parse(p[3])).Select(p => p[2]).ToArray();
        if (index[2] != "0" || index[3] != "0" || keys.Length != 1 || keys[0] != "CreatedAt")
        {
            unsafeDrift.Add($"index '{IndexName}' is not a plain non-unique, unfiltered index on (CreatedAt)");
        }

        return false;
    }

    static string ExpectedType(DatabaseType databaseType, string column)
    {
        var (id, tableName, number) = databaseType switch
        {
            DatabaseType.PostgreSql => ("uuid", "text", "bigint"),
            DatabaseType.SqlServer => ("uniqueidentifier", "nvarchar(max)", "bigint"),
            _ => ("TEXT", "TEXT", "INTEGER")
        };
        return ExpectedTypes((id, tableName, number)).GetValueOrDefault(column, number);
    }

    static Dictionary<string, string> ExpectedTypes((string Id, string TableName, string Number) types) =>
        new(StringComparer.Ordinal) { ["Id"] = types.Id, ["TableName"] = types.TableName, ["EventSequenceNumber"] = types.Number, ["CreatedAt"] = types.Number };

    /// <summary>
    /// Compares only what catalogs reformat for the unchanged clock expression: case, spacing, parentheses and numeric casts.
    /// </summary>
    /// <param name="expression">The default expression.</param>
    /// <returns>The comparable form.</returns>
    static string Normalize(string expression) =>
        new string(expression.Where(character => !char.IsWhiteSpace(character) && character is not '(' and not ')').ToArray())
            .ToLowerInvariant()
            .Replace("::numeric", string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// The outcome of inspecting the receipt table.
    /// </summary>
    /// <param name="TableMissing">Whether the table does not exist.</param>
    /// <param name="Repairable">The discrepancies offline provisioning can add without changing existing objects.</param>
    /// <param name="Unsafe">The drift that provisioning must not touch.</param>
    sealed record ReceiptInspection(bool TableMissing, IReadOnlyList<string> Repairable, IReadOnlyList<string> Unsafe)
    {
        /// <summary>
        /// Gets whether nothing is missing or drifted.
        /// </summary>
        internal bool IsCurrent => !TableMissing && Repairable.Count == 0 && Unsafe.Count == 0;

        /// <summary>
        /// Gets every discrepancy.
        /// </summary>
        internal IReadOnlyList<string> All => [.. Repairable, .. Unsafe];
    }
}
