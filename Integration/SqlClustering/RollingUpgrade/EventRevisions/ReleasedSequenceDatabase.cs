// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

internal static class ReleasedSequenceDatabase
{
    /// <summary>SchemaTables is internal to Storage.Sql. Use its sequence shape, then require our actual witness row.</summary>
    static readonly string[] _eventColumns = ["SequenceNumber", "EventSourceId", "EventSourceType", "Type", "Content", "Occurred", "Causation", "CausedBy", "CorrelationId"];

    internal static async Task<(string Connection, string Table)> Find(SqlClusterDatabase database, bool postgres, string witness)
    {
        await using var root = database.CreateConnection();
        var rootConnectionString = root.ConnectionString;
        await root.OpenAsync();
        var databases = await ReadDatabases(root, postgres);
        foreach (var name in databases)
        {
            var connectionString = postgres
                ? new NpgsqlConnectionStringBuilder(rootConnectionString) { Database = name }.ConnectionString
                : new SqlConnectionStringBuilder(rootConnectionString) { InitialCatalog = name }.ConnectionString;
            await using var connection = Connect(connectionString, postgres);
            await connection.OpenAsync();
            foreach (var table in await Tables(connection))
            {
                if (!_eventColumns.All(column => table.Columns.Contains(column, StringComparer.Ordinal))) continue;
                await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Catalog identifiers are delimited and escaped; the witness is a parameter.
                command.CommandText = $"SELECT COUNT(*) FROM {Quote(table.Schema)}.{Quote(table.Name)} WHERE {Quote("EventSourceId")} = @witness";
#pragma warning restore CA2100
                var parameter = command.CreateParameter();
                parameter.ParameterName = "witness";
                parameter.Value = witness;
                command.Parameters.Add(parameter);
                if (Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) == 0) continue;
                if (postgres) connectionString = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = table.Schema }.ConnectionString;
                else table.Schema.ShouldEqual("dbo");
                return (connectionString, table.Name);
            }
        }
        throw new ReleasedSequenceNotFound();
    }

    static DbConnection Connect(string connectionString, bool postgres) => postgres
        ? new NpgsqlConnection(connectionString)
        : new SqlConnection(connectionString);

    static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    static async Task<string[]> ReadDatabases(DbConnection connection, bool postgres)
    {
        await using var command = connection.CreateCommand();
        if (postgres) command.CommandText = "SELECT datname FROM pg_database WHERE NOT datistemplate";
        else command.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4";
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values.ToArray();
    }

    static async Task<IEnumerable<(string Schema, string Name, string[] Columns)>> Tables(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<(string Schema, string Table, string Column)>();
        while (await reader.ReadAsync()) columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        return columns.GroupBy(column => (column.Schema, column.Table))
            .Select(group => (group.Key.Schema, group.Key.Table, group.Select(column => column.Column).ToArray())).ToArray();
    }
}
