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
/// with. The statements run as one batch that does nothing when the columns are already exact. Other providers already
/// compare exactly and are left unchanged.
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
            $"IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'{Table}') AND [name] IN ({ColumnNames}) AND [collation_name] <> N'{ClosedStreamColumn.Collation}')",
            "BEGIN",
            $"ALTER TABLE {Table} DROP CONSTRAINT {PrimaryKey};",
            .. _columns.Select(column => $"ALTER TABLE {Table} ALTER COLUMN [{column}] nvarchar({ClosedStreamColumn.MaxLength}) COLLATE {ClosedStreamColumn.Collation} NOT NULL;"),
            $"UPDATE {Table} SET {Assignments(column => $"CASE WHEN {EndsWithSpaceOrTerminator(column)} THEN [{column}] + NCHAR(1) ELSE [{column}] END")} WHERE {Any(EndsWithSpaceOrTerminator)};",
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
            $"IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'{Table}') AND [name] IN ({ColumnNames}) AND [collation_name] = N'{ClosedStreamColumn.Collation}')",
            "BEGIN",
            $"ALTER TABLE {Table} DROP CONSTRAINT {PrimaryKey};",
            $"UPDATE {Table} SET {Assignments(column => $"CASE WHEN {EndsWithTerminator(column)} THEN LEFT([{column}], DATALENGTH([{column}]) / 2 - 1) ELSE [{column}] END")} WHERE {Any(EndsWithTerminator)};",
            .. _columns.Select(column => $"ALTER TABLE {Table} ALTER COLUMN [{column}] nvarchar(255) COLLATE DATABASE_DEFAULT NOT NULL;"),
            $"ALTER TABLE {Table} ADD CONSTRAINT {PrimaryKey} PRIMARY KEY ({PrimaryKeyColumns});",
            "END"
        ];
        migrationBuilder.Sql(string.Join('\n', statements));
    }

    static string Any(Func<string, string> predicate) => string.Join(" OR ", _columns.Select(predicate));

    static string EndsWithSpaceOrTerminator(string column) => $"UNICODE(RIGHT([{column}], 1)) IN (32, 1)";

    static string EndsWithTerminator(string column) => $"UNICODE(RIGHT([{column}], 1)) = 1";

    static string Assignments(Func<string, string> value) => string.Join(", ", _columns.Select(column => $"[{column}] = {value(column)}"));
}
