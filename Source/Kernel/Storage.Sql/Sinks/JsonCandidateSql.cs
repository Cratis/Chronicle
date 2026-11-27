// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Builds advisory descendant predicates using delimited identifiers and parameter placeholders.
/// </summary>
internal static class JsonCandidateSql
{
    /// <summary>
    /// Creates a candidate query while leaving exact identity comparison to the path walker.
    /// </summary>
    /// <param name="type">The SQL provider.</param>
    /// <param name="table">The delimited table name.</param>
    /// <param name="column">The delimited JSON column name.</param>
    /// <param name="key">The delimited root key column name.</param>
    /// <param name="stringsOnly">Whether a composite identity supplied an exact string leaf.</param>
    /// <returns>The query, or null for unsupported providers.</returns>
    internal static string? Create(DatabaseType type, string table, string column, string key, bool stringsOnly) => type switch
    {
        DatabaseType.Sqlite => Sqlite(table, column, stringsOnly),
        DatabaseType.PostgreSql => PostgreSql(table, column, stringsOnly),
        DatabaseType.SqlServer => SqlServer(table, column, key, stringsOnly),
        _ => null
    };

    static string Sqlite(string table, string column, bool stringsOnly)
    {
        // Composite identities may themselves be stored as JSON text inside a string node.
        // Admit any string containing an opening brace/bracket rather than parsing it in SQL.
        // This is deliberately broad: only the in-memory structural comparison decides a match.
        var other = stringsOnly
            ? "(j.type='text' AND (instr(j.atom,char(123))>0 OR instr(j.atom,char(91))>0))"
            : "j.type IN ('integer','real','true','false')";
        return $"SELECT t.* FROM {table} t WHERE EXISTS (SELECT 1 FROM json_tree(CASE WHEN json_valid(t.{column}) THEN t.{column} ELSE NULL END) j WHERE {other} OR j.atom = {{0}} OR j.atom = {{1}})";
    }

    static string PostgreSql(string table, string column, bool stringsOnly)
    {
        var leaves = stringsOnly
            ? "@ == $v || @ == $w"
            : "@.type() == \"number\" || @.type() == \"boolean\" || @ == $v || @ == $w";
        var encoded = stringsOnly
            ? $" OR EXISTS (SELECT 1 FROM jsonb_path_query(t.{column}, '$.**') j(value) WHERE jsonb_typeof(j.value)='string' AND (strpos(j.value::text,chr(123))>0 OR strpos(j.value::text,chr(91))>0))"
            : string.Empty;
        return $"SELECT t.* FROM {table} t WHERE jsonb_path_exists(t.{column}, '$.** ? ({leaves})', jsonb_build_object('v', CAST({{0}} AS text), 'w', CAST({{1}} AS text))){encoded}";
    }

    static string SqlServer(string table, string column, string key, bool stringsOnly)
    {
        var other = stringsOnly
            ? "(n.kind=1 AND (CHARINDEX(NCHAR(123),n.node)>0 OR CHARINDEX(NCHAR(91),n.node)>0))"
            : "n.kind IN (2,3)";
        return $"WITH json_nodes AS (SELECT {key} AS row_id, CAST({column} AS nvarchar(max)) AS node, CAST(5 AS int) AS kind FROM {table} WHERE ISJSON({column})=1 UNION ALL SELECT n.row_id, j.value, CAST(j.type AS int) FROM json_nodes n CROSS APPLY OPENJSON(CASE WHEN n.kind IN (4,5) THEN n.node ELSE N'{{{{}}}}' END) j) SELECT t.* FROM {table} t WHERE EXISTS (SELECT 1 FROM json_nodes n WHERE n.row_id=t.{key} AND ({other} OR (n.kind=1 AND (n.node={{0}} OR n.node={{1}})))) OPTION (MAXRECURSION 0)";
    }
}
