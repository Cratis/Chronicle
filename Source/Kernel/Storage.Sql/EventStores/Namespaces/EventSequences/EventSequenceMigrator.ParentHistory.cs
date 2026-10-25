// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceMigrator
{
    /// <summary>
    /// Gets a parent-history index name that fits PostgreSQL's identifier limit.
    /// </summary>
    /// <param name="tableName">The event sequence table.</param>
    /// <returns>The index name.</returns>
    internal static string ParentHistoryIndexName(string tableName)
    {
        var name = $"IX_{tableName}_parentHistory";
        if (Encoding.UTF8.GetByteCount(name) <= 63) return name;
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tableName)))[..16];
        return $"IX_{hash}_parentHistory";
    }

    static void AddParentHistoryIndex(MigrationBuilder migration, string tableName) =>
        migration.CreateIndex(
            name: ParentHistoryIndexName(tableName),
            table: tableName,
            columns: [nameof(EventEntry.EventSourceId), nameof(EventEntry.Type), nameof(EventEntry.SequenceNumber)]);

    static async Task<bool> ParentHistoryIndexExists(EventSequenceDbContext context, string tableName)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            switch (context.Database.ProviderName)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    command.CommandText = "SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = SCHEMA_NAME() AND t.name = @table AND i.name = @index";
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    command.CommandText = "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = current_schema() AND tablename = @table AND indexname = @index";
                    break;
                default:
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name = @table AND name = @index";
                    break;
            }
            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@table";
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);
            var indexParameter = command.CreateParameter();
            indexParameter.ParameterName = "@index";
            indexParameter.Value = ParentHistoryIndexName(tableName);
            command.Parameters.Add(indexParameter);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    async Task EnsureParentHistoryIndex(EventSequenceDbContext context, string tableName)
    {
        if (!await HasParentHistoryColumns(context, tableName) || await ParentHistoryIndexExists(context, tableName))
        {
            return;
        }

        var migration = new MigrationBuilder(context.Database.ProviderName);
        AddParentHistoryIndex(migration, tableName);
        await tableMigrator.ExecuteMigrationOperations(context, migration);
    }

    async Task<bool> HasParentHistoryColumns(EventSequenceDbContext context, string tableName)
    {
        // Partial legacy schemas cannot support event reads until their source/type columns exist.
        return await tableMigrator.ColumnExists(context, tableName, nameof(EventEntry.EventSourceId)) &&
            await tableMigrator.ColumnExists(context, tableName, nameof(EventEntry.Type));
    }
}
