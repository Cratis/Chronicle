// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Replays fixture SELECTs as EXPLAIN commands on a fresh connection.</summary>
/// <param name="provider">The SQL provider.</param>
/// <param name="connectionString">The fixture database connection string.</param>
internal sealed partial class SqlPlanQueries(StorageProvider provider, string connectionString)
{
    [GeneratedRegex("FROM\\s+(?:(?:\"[^\"]+\"|\\[[^\\]]+\\]|[A-Za-z_][A-Za-z0-9_$]*)\\s*\\.\\s*)?(?:\"(?<table>[^\"]+)\"|\\[(?<table>[^\\]]+)\\]|(?<table>[A-Za-z_][A-Za-z0-9_$]*))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TableName { get; }

    /// <summary>Explains a captured SELECT without modifying the measured command counter.</summary>
    /// <param name="query">The captured command and parameters.</param>
    /// <param name="rawPlanPath">Optional destination for the raw SQL Server showplan XML.</param>
    /// <returns>The database plan, or the failure as text.</returns>
    public Task<string> Explain(CapturedQuery query, string? rawPlanPath = null) => provider switch
    {
        StorageProvider.Sqlite => Execute("EXPLAIN QUERY PLAN " + query.Sql, query.Parameters, 3),
        StorageProvider.PostgreSql => Execute("EXPLAIN (ANALYZE, BUFFERS) " + query.Sql, query.Parameters, 0),
        StorageProvider.SqlServer => SqlServerPlans.Capture(connectionString, query, rawPlanPath),
        _ => Task.FromResult("not captured")
    };

    /// <summary>Lists indexes for the first table in the generated SELECT.</summary>
    /// <param name="query">The captured SELECT.</param>
    /// <returns>The index definitions, or a failure as text.</returns>
    public Task<string> IndexDefinitions(CapturedQuery query)
    {
        var table = TableOf(query.Sql);
        if (table.Length == 0) return Task.FromResult("not captured: table name not found");
        var sql = provider switch
        {
            StorageProvider.Sqlite => "SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name=@table",
            StorageProvider.SqlServer => """
                SELECT SCHEMA_NAME(t.schema_id), t.name, i.name, i.type_desc, i.is_unique,
                       c.name, ic.key_ordinal, ic.is_descending_key, ic.is_included_column, i.filter_definition
                FROM sys.indexes i
                JOIN sys.tables t ON t.object_id=i.object_id
                JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE t.name=@table
                ORDER BY i.index_id, ic.index_column_id
                """,
            _ => "SELECT indexname, indexdef FROM pg_indexes WHERE tablename=@table"
        };
        return Execute(sql, [new("@table", table, System.Data.DbType.String, null)], -1);
    }

    /// <summary>Finds the fixture table in a captured SELECT, including bracketed SQL Server identifiers.</summary>
    /// <param name="sql">The captured SELECT.</param>
    /// <returns>The unquoted table name, or empty when it cannot be found.</returns>
    internal static string TableOf(string sql) => TableName.Match(sql).Groups["table"].Value;

    /// <summary>
    /// Restores captured values and provider-specific parameter metadata.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="source">The captured parameter.</param>
    internal static void AddParameter(DbCommand command, CapturedParameter source)
    {
        var parameter = source.ProviderParameter switch
        {
            NpgsqlParameter postgres => postgres.Clone(),
            SqlParameter sqlServer => (DbParameter)((ICloneable)sqlServer).Clone(),
            _ => command.CreateParameter()
        };
        parameter.ParameterName = source.Name;
        if (source.ProviderParameter is null) parameter.DbType = source.DbType;
        parameter.Value = source.Value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    DbConnection CreateConnection() => provider switch
    {
        StorageProvider.Sqlite => new SqliteConnection(connectionString),
        StorageProvider.SqlServer => new SqlConnection(connectionString),
        _ => new NpgsqlConnection(connectionString)
    };

    [SuppressMessage("Security", "CA2100", Justification = "Only captured EF-generated fixture SELECTs prefixed with EXPLAIN or constant catalog queries reach this method; values are parameterized and no user SQL is accepted.")]
    async Task<string> Execute(string sql, IReadOnlyList<CapturedParameter> parameters, int column)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(RunBudget.Current.Token);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;
            foreach (var parameter in parameters) AddParameter(command, parameter);
            await using var reader = await command.ExecuteReaderAsync(RunBudget.Current.Token);
            var rows = new List<string>();
            while (await reader.ReadAsync(RunBudget.Current.Token))
            {
                rows.Add(column < 0 ? string.Join(": ", Enumerable.Range(0, reader.FieldCount).Select(index => Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture))) : reader.GetString(column));
            }

            return string.Join('\n', rows);
        }
        catch (Exception error)
        {
            return $"not captured: {error.Message}";
        }
    }
}
