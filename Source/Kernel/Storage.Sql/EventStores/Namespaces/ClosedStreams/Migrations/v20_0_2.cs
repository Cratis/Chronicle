// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Makes the stream type, stream id, event source id and event source type of closed streams compare exactly on SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// The four columns move to <see cref="ClosedStreamColumn.Collation"/> and grow by one character so a value can carry the
/// terminator described by <see cref="ClosedStreamColumn"/>. Values that end with a space or with the terminator get the
/// terminator appended before the primary key is created again. The earlier collation was the one that merged spellings,
/// so no two stored rows can collide: nothing is merged, dropped or re-keyed, rows only keep the spelling they were stored
/// with. The statements run as one transactional batch. Each column's width marks whether its values have been encoded,
/// independently of its collation (the database default may already be binary). An already widened column is not encoded
/// again, even when another column still needs upgrading. Other providers already compare exactly and are left unchanged.
/// </para>
/// <para>
/// Going down strips the terminators and restores the database default collation. That fails on the primary key when the
/// table by then holds spellings the default collation considers equal.
/// </para>
/// </remarks>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ClosedStreams}-{nameof(v20_0_2)}")]
public class v20_0_2 : Migration
{
    const string Table = $"[{WellKnownTableNames.ClosedStreams}]";
    const string PrimaryKey = $"[PK_{WellKnownTableNames.ClosedStreams}]";
    const string PrimaryKeyColumns = "[EventSequenceId], [StreamType], [StreamId], [EventSourceId], [EventSourceType], [Owner], [Dimensions]";
    static readonly string[] _columns = ["StreamType", "StreamId", "EventSourceId", "EventSourceType"];

    static string ColumnNames => string.Join(", ", _columns.Select(column => $"N'{column}'"));

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        string[] statements =
        [
            $"IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'{Table}') AND [name] IN ({ColumnNames}) AND ([collation_name] <> N'{ClosedStreamColumn.Collation}' OR [max_length] <> {ClosedStreamColumn.MaxLength * 2}))",
            "BEGIN",
            $"ALTER TABLE {Table} DROP CONSTRAINT {PrimaryKey};",
            .. _columns.Select(column => EncodingFlag(column, "<>")),
            .. _columns.Select(column => $"ALTER TABLE {Table} ALTER COLUMN [{column}] nvarchar({ClosedStreamColumn.MaxLength}) COLLATE {ClosedStreamColumn.Collation} NOT NULL;"),
            $"UPDATE {Table} SET {Assignments(column => $"CASE WHEN {NeedsEncoding(column)} THEN [{column}] + NCHAR(1) ELSE [{column}] END")} WHERE {Any(NeedsEncoding)};",
            $"ALTER TABLE {Table} ADD CONSTRAINT {PrimaryKey} PRIMARY KEY ({PrimaryKeyColumns});",
            "END"
        ];
        migrationBuilder.Sql(string.Join('\n', statements));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        string[] statements =
        [
            $"IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'{Table}') AND [name] IN ({ColumnNames}) AND [max_length] = {ClosedStreamColumn.MaxLength * 2})",
            "BEGIN",
            $"ALTER TABLE {Table} DROP CONSTRAINT {PrimaryKey};",
            .. _columns.Select(column => EncodingFlag(column, "=")),
            $"UPDATE {Table} SET {Assignments(column => $"CASE WHEN {NeedsDecoding(column)} THEN LEFT([{column}], DATALENGTH([{column}]) / 2 - 1) ELSE [{column}] END")} WHERE {Any(NeedsDecoding)};",
            .. _columns.Select(column => $"ALTER TABLE {Table} ALTER COLUMN [{column}] nvarchar(255) COLLATE DATABASE_DEFAULT NOT NULL;"),
            $"ALTER TABLE {Table} ADD CONSTRAINT {PrimaryKey} PRIMARY KEY ({PrimaryKeyColumns});",
            "END"
        ];
        migrationBuilder.Sql(string.Join('\n', statements));
    }

    static string EncodingFlag(string column, string comparison) => $"DECLARE @Encode{column} bit = CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'{Table}') AND [name] = N'{column}' AND [max_length] {comparison} {ClosedStreamColumn.MaxLength * 2}) THEN 1 ELSE 0 END;";

    static string NeedsEncoding(string column) => $"@Encode{column} = 1 AND {EndsWithSpaceOrTerminator(column)}";

    static string NeedsDecoding(string column) => $"@Encode{column} = 1 AND {EndsWithTerminator(column)}";

    static string Any(Func<string, string> predicate) => string.Join(" OR ", _columns.Select(predicate));

    static string EndsWithSpaceOrTerminator(string column) => $"UNICODE(RIGHT([{column}], 1)) IN (32, 1)";

    static string EndsWithTerminator(string column) => $"UNICODE(RIGHT([{column}], 1)) = 1";

    static string Assignments(Func<string, string> value) => string.Join(", ", _columns.Select(column => $"[{column}] = {value(column)}"));
}
