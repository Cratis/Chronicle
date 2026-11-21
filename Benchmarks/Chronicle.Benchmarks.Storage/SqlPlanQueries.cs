// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Replays fixture SELECTs as EXPLAIN commands on a fresh connection.</summary>
/// <param name="provider">The SQL provider.</param>
/// <param name="connectionString">The fixture database connection string.</param>
internal sealed partial class SqlPlanQueries(StorageProvider provider, string connectionString)
{
    [GeneratedRegex("FROM\\s+\"(?<table>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TableName { get; }

    /// <summary>Explains a captured SELECT without modifying the measured command counter.</summary>
    /// <param name="query">The captured command and parameters.</param>
    /// <returns>The database plan, or the failure as text.</returns>
    public Task<string> Explain(CapturedQuery query) => provider switch
    {
        StorageProvider.Sqlite => Execute("EXPLAIN QUERY PLAN " + query.Sql, query.Parameters, 3),
        StorageProvider.PostgreSql => Execute("EXPLAIN (ANALYZE, BUFFERS) " + query.Sql, query.Parameters, 0),
        _ => Task.FromResult("not captured")
    };

    /// <summary>Lists indexes for the first quoted table in the generated SELECT.</summary>
    /// <param name="query">The captured SELECT.</param>
    /// <returns>The index definitions, or a failure as text.</returns>
    public Task<string> IndexDefinitions(CapturedQuery query)
    {
        var table = TableName.Match(query.Sql).Groups["table"].Value;
        if (table.Length == 0) return Task.FromResult("not captured: table name not found");
        var sql = provider == StorageProvider.Sqlite
            ? "SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name=@table"
            : "SELECT indexname, indexdef FROM pg_indexes WHERE tablename=@table";
        return Execute(sql, [new("@table", table, System.Data.DbType.String, null)], -1);
    }

    static void AddParameter(DbCommand command, CapturedParameter source)
    {
        var parameter = source.ProviderParameter is NpgsqlParameter postgres ? postgres.Clone() : command.CreateParameter();
        parameter.ParameterName = source.Name;
        if (source.ProviderParameter is null) parameter.DbType = source.DbType;
        parameter.Value = source.Value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    [SuppressMessage("Security", "CA2100", Justification = "Only captured EF-generated fixture SELECTs prefixed with EXPLAIN or constant catalog queries reach this method; values are parameterized and no user SQL is accepted.")]
    async Task<string> Execute(string sql, IReadOnlyList<CapturedParameter> parameters, int column)
    {
        try
        {
            await using DbConnection connection = provider == StorageProvider.Sqlite ? new SqliteConnection(connectionString) : new NpgsqlConnection(connectionString);
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
