// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// The SQL Server spelling review: finds identifiers that exist in more than one spelling and remembers the counts.
/// </summary>
public partial class EventSequenceMigrator
{
    /// <summary>The reserved table holding one spelling review row per event sequence table on SQL Server.</summary>
    public const string SpellingReviewsTable = "__cratis_spelling_reviews";

    const string MergeReviewSql =
        "MERGE [" + SpellingReviewsTable + "] WITH (HOLDLOCK) AS target USING (SELECT @table AS TableName) AS source ON target.TableName = source.TableName "
        + "WHEN MATCHED THEN UPDATE SET Reviewed = CASE WHEN target.EventSourceIds = @sources AND target.EventStreamIds = @streams THEN target.Reviewed ELSE 0 END, EventSourceIds = @sources, EventStreamIds = @streams, CheckedAt = SYSDATETIMEOFFSET() "
        + "WHEN NOT MATCHED THEN INSERT (TableName, EventSourceIds, EventStreamIds, CheckedAt, Reviewed) VALUES (@table, @sources, @streams, SYSDATETIMEOFFSET(), 0);";

    /// <inheritdoc/>
    public async Task CheckSpelling(EventSequenceDbContext context, string tableName)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        var table = QuoteName(tableName);
        var sourceIds = await Scalar(context, $"SELECT COUNT_BIG(*) FROM (SELECT EventSourceId FROM {table} GROUP BY EventSourceId HAVING MIN(CAST(EventSourceId AS varbinary(400))) <> MAX(CAST(EventSourceId AS varbinary(400)))) x", null, 0);
        var streamIds = await Scalar(context, $"SELECT COUNT_BIG(*) FROM (SELECT EventStreamType, EventStreamId FROM {table} GROUP BY EventStreamType, EventStreamId HAVING MIN(CAST(EventStreamType AS varbinary(400))) <> MAX(CAST(EventStreamType AS varbinary(400))) OR MIN(CAST(EventStreamId AS varbinary(400))) <> MAX(CAST(EventStreamId AS varbinary(400)))) x", null, 0);

        await EnsureSpellingReviewsTable(context);
        await Scalar(
            context,
            MergeReviewSql,
            [("table", tableName), ("sources", sourceIds), ("streams", streamIds)],
            30);

        if (sourceIds > 0 || streamIds > 0)
        {
            logger.SpellingVariantsFound(tableName, sourceIds, streamIds);
        }
        else
        {
            logger.NoSpellingVariants(tableName);
        }
    }

    static string QuoteName(string name) => $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

    static async Task<long> Scalar(EventSequenceDbContext context, string sql, (string Name, object Value)[]? parameters, int timeoutSeconds)
    {
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The table name is quoted; every value is a parameter.
            command.CommandText = sql;
#pragma warning restore CA2100
            command.CommandTimeout = timeoutSeconds;
            foreach (var (name, value) in parameters ?? [])
            {
                AddParameter(command, name, value);
            }

            var result = await command.ExecuteScalarAsync();
            return result is null or DBNull ? 0 : Convert.ToInt64(result);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = $"@{name}";
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    async Task EnsureSpellingReviewsTable(EventSequenceDbContext context)
    {
        if (await TableMigrator<EventSequenceDbContext>.TableExists(context, SpellingReviewsTable))
        {
            return;
        }

        try
        {
            await Scalar(
                context,
                $"CREATE TABLE [{SpellingReviewsTable}] (TableName nvarchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL PRIMARY KEY, EventSourceIds bigint NOT NULL, EventStreamIds bigint NOT NULL, CheckedAt datetimeoffset NOT NULL, Reviewed bit NOT NULL DEFAULT 0)",
                null,
                30);
        }
        catch (Exception exception) when (IsTableAlreadyExists(exception))
        {
            // Another node created it first.
        }
    }

    async Task ReportSpellingReview(EventSequenceDbContext context, string tableName)
    {
        if (!context.Database.IsSqlServer() || !await TableMigrator<EventSequenceDbContext>.TableExists(context, SpellingReviewsTable))
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT EventSourceIds, EventStreamIds, Reviewed FROM [{SpellingReviewsTable}] WHERE TableName = @table";
            AddParameter(command, "table", tableName);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                logger.SpellingNotChecked(tableName);
                return;
            }

            var sourceIds = reader.GetInt64(0);
            var streamIds = reader.GetInt64(1);
            if ((sourceIds > 0 || streamIds > 0) && !reader.GetBoolean(2))
            {
                logger.SpellingVariantsUnreviewed(tableName, sourceIds, streamIds);
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
