// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Describes how SQL Server tables of unique constraint claims keep the owner of a claim exactly.
/// </summary>
/// <remarks>
/// The owner column follows the default collation of the database, which ignores letter case, width, accents and trailing spaces.
/// A persisted hash of the stored bytes of the owner is added to the key so every spelling keeps its own row. Rows written before
/// the hash existed are pending: they are marked ExactSpelling = 0 until the constraint index is rebuilt from the event log.
/// </remarks>
static class UniqueConstraintSpellings
{
    /// <summary>
    /// The name of the hash column on per-event-source tables.
    /// </summary>
    internal const string ExactKeyColumn = nameof(UniqueConstraintIndexEntry.ExactKey);

    /// <summary>
    /// The name of the column telling whether the owner of a row is known exactly.
    /// </summary>
    internal const string ExactSpellingColumn = nameof(UniqueConstraintIndexEntry.ExactSpelling);

    /// <summary>
    /// The expression of the persisted hash over the stored bytes of the owner. A binary cast keeps trailing spaces.
    /// </summary>
    internal const string ExactKeyExpression = "HASHBYTES('SHA2_256', CAST([EventSourceId] AS varbinary(400)))";

    /// <summary>
    /// Delimits an identifier for SQL Server.
    /// </summary>
    /// <param name="name">The identifier.</param>
    /// <returns>The delimited identifier.</returns>
    internal static string Delimit(string name) => $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

    /// <summary>
    /// Writes a string as a unicode literal for SQL Server.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <returns>The literal.</returns>
    internal static string Literal(string value) => $"N'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    /// <summary>
    /// Builds the batch that moves a per-event-source table to the exact owner key in one transaction.
    /// </summary>
    /// <param name="tableName">The table to upgrade.</param>
    /// <returns>The batch.</returns>
    /// <remarks>
    /// Nothing happens when the key hash exists already. Rows are never rewritten; they keep ExactSpelling = 0.
    /// </remarks>
    internal static string AddExactOwnerKey(string tableName)
    {
        var table = Delimit(tableName);
        var tableLiteral = Literal(table);
        var primaryKey = Delimit($"PK_{tableName}");
        return string.Join(
            "\n",
            "SET XACT_ABORT ON;",
            "BEGIN TRANSACTION;",
            $"IF COL_LENGTH({tableLiteral}, N'{ExactKeyColumn}') IS NULL",
            "BEGIN",
            $"    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID({tableLiteral}) AND name = N'EventSourceId' AND system_type_id = 231 AND max_length = 400)",
            "        THROW 50000, N'The unique constraint table has an unexpected owner column. Nothing changed.', 1;",
            $"    IF COL_LENGTH({tableLiteral}, N'{ExactSpellingColumn}') IS NULL",
            $"        EXEC({Literal($"ALTER TABLE {table} ADD [{ExactSpellingColumn}] bit NOT NULL DEFAULT 0")});",
            $"    EXEC({Literal($"ALTER TABLE {table} ADD [{ExactKeyColumn}] AS ({ExactKeyExpression}) PERSISTED NOT NULL")});",
            $"    DECLARE @key sysname = (SELECT TOP 1 name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID({tableLiteral}) AND type = N'PK');",
            "    DECLARE @drop nvarchar(max) = NULL;",
            $"    IF @key IS NOT NULL SET @drop = N'ALTER TABLE ' + {Literal(table)} + N' DROP CONSTRAINT ' + QUOTENAME(@key);",
            "    IF @drop IS NOT NULL EXEC(@drop);",
            $"    EXEC({Literal($"ALTER TABLE {table} ADD CONSTRAINT {primaryKey} PRIMARY KEY ([EventSourceId], [{ExactKeyColumn}])")});",
            "END;",
            "COMMIT TRANSACTION;");
    }

    /// <summary>
    /// Builds the batch that marks a per-value table with the exact spelling column.
    /// </summary>
    /// <param name="tableName">The table to upgrade.</param>
    /// <returns>The batch.</returns>
    internal static string AddExactSpelling(string tableName)
    {
        var table = Delimit(tableName);
        return $"IF COL_LENGTH({Literal(table)}, N'{ExactSpellingColumn}') IS NULL ALTER TABLE {table} ADD [{ExactSpellingColumn}] bit NOT NULL DEFAULT 0;";
    }

    /// <summary>
    /// Counts the rows of a table that are pending, meaning that their owner is not known exactly.
    /// </summary>
    /// <param name="context">The context connected to the namespace database.</param>
    /// <param name="tableName">The table to count.</param>
    /// <param name="cancellationToken">Cancellation of the read.</param>
    /// <returns>The number of pending rows. Every row counts when the table does not have the column yet.</returns>
    internal static async Task<long> CountPending(DbContext context, string tableName, CancellationToken cancellationToken = default)
    {
        var table = Delimit(tableName);
        var hasColumn = $"SELECT CASE WHEN COL_LENGTH({Literal(table)}, N'{ExactSpellingColumn}') IS NULL THEN CAST(0 AS bigint) ELSE CAST(1 AS bigint) END AS [Value]";
#pragma warning disable EF1002 // The table name is delimited above.
        var upgraded = await context.Database.SqlQueryRaw<long>(hasColumn).SingleAsync(cancellationToken) == 1;
        var count = upgraded
            ? $"SELECT COUNT_BIG(*) AS [Value] FROM {table} WHERE [{ExactSpellingColumn}] = 0"
            : $"SELECT COUNT_BIG(*) AS [Value] FROM {table}";
        return await context.Database.SqlQueryRaw<long>(count).SingleAsync(cancellationToken);
#pragma warning restore EF1002
    }
}
