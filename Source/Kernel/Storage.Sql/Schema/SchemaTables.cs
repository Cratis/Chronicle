// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Reads table names and column shapes in the connected schema.
/// </summary>
internal static class SchemaTables
{
    static readonly string[] _eventColumns = ["SequenceNumber", "EventSourceId", "EventSourceType", "Type", "Content", "Occurred", "Causation", "CausedBy", "CorrelationId"];

    /// <summary>
    /// Determines whether columns describe an event-sequence table.
    /// </summary>
    /// <param name="columns">The column names.</param>
    /// <returns>Whether all required event columns exist.</returns>
    internal static bool IsSequence(IReadOnlyList<string> columns) => _eventColumns.All(column => columns.Contains(column, StringComparer.Ordinal));

    /// <summary>
    /// Reads the table columns from the provider catalog.
    /// </summary>
    /// <param name="context">The connected context.</param>
    /// <returns>The table names and their columns.</returns>
    internal static async Task<IReadOnlyDictionary<string, string[]>> Read(DbContext context)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            switch (context.Database.ProviderName)
            {
                case "Microsoft.EntityFrameworkCore.Sqlite":
                    command.CommandText = "SELECT m.name, p.name FROM sqlite_master m JOIN pragma_table_info(m.name) p WHERE m.type = 'table'";
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    command.CommandText = "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME()";
                    break;
                default:
                    command.CommandText = "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = current_schema()";
                    break;
            }
            await using var reader = await command.ExecuteReaderAsync();
            var columns = new List<(string Table, string Column)>();
            while (await reader.ReadAsync())
            {
                columns.Add((reader.GetString(0), reader.GetString(1)));
            }

            return columns.GroupBy(column => column.Table, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(column => column.Column).ToArray(), StringComparer.Ordinal);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
