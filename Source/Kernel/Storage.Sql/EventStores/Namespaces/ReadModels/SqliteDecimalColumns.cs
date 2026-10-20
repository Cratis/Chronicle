// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Identifies decimal columns whose existing SQLite text affinity must be preserved.
/// </summary>
internal static class SqliteDecimalColumns
{
    /// <summary>
    /// Reads legacy decimal storage before EF constructs its model, without creating a database.
    /// </summary>
    /// <param name="options">The context's connection options.</param>
    /// <param name="table">The physical table name.</param>
    /// <param name="columns">The projected column definitions.</param>
    /// <returns>The decimal columns with existing text affinity.</returns>
    internal static async Task<IReadOnlySet<string>> Read(DbContextOptions options, string table, IReadOnlyList<ProjectedColumn> columns)
    {
        var textColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var decimals = columns.Where(column => (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType) == typeof(decimal)).Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (decimals.Count == 0) return textColumns;
        var relational = options.Extensions.OfType<RelationalOptionsExtension>().SingleOrDefault();
        if (relational is null || (relational.Connection is not SqliteConnection && relational.ConnectionString?.GetDatabaseType() != DatabaseType.Sqlite)) return textColumns;
        var supplied = relational.Connection as SqliteConnection;
        var builder = new SqliteConnectionStringBuilder(supplied?.ConnectionString ?? relational.ConnectionString ?? string.Empty);
        if (supplied?.State != ConnectionState.Open) supplied = null;
        if (supplied is null && builder.Mode != SqliteOpenMode.Memory && builder.DataSource != ":memory:") builder.Mode = SqliteOpenMode.ReadOnly;
        await using var owned = supplied is null ? new SqliteConnection(builder.ConnectionString) : null;
        var connection = supplied ?? owned!;
        try
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        }
        catch (SqliteException exception) when (supplied is null && exception.SqliteErrorCode == 14 && !File.Exists(builder.DataSource))
        {
            // An absent database has no legacy columns. Provisioning still decides whether
            // it may be created; this probe must not create a file ahead of that decision.
            return textColumns;
        }
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, type FROM pragma_table_info($table)";
        command.Parameters.AddWithValue("$table", table);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var type = reader.GetString(1);
            if (decimals.Contains(reader.GetString(0)) && (type.Contains("TEXT", StringComparison.OrdinalIgnoreCase) || type.Contains("CHAR", StringComparison.OrdinalIgnoreCase) || type.Contains("CLOB", StringComparison.OrdinalIgnoreCase)))
            {
                textColumns.Add(reader.GetString(0));
            }
        }
        return textColumns;
    }
}
