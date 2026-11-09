// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

public class when_upgrading_for_sql_server : Specification
{
    static readonly string[] _columns = ["StreamType", "StreamId", "EventSourceId", "EventSourceType"];
    SqlOperation _operation;
    string[] _statements;

    void Because()
    {
        _operation = new v20_0_2 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.UpOperations
            .Cast<SqlOperation>()
            .Single();
        _statements = _operation.Sql.Split('\n');
    }

    int IndexOf(string start) => Array.FindIndex(_statements, _ => _.StartsWith(start, StringComparison.Ordinal));

    [Fact]
    void should_upgrade_legacy_columns_even_when_the_database_and_columns_already_use_binary_collation() =>
        _statements[0].ShouldEqual("IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[ClosedStreams]') AND [name] IN (N'StreamType', N'StreamId', N'EventSourceId', N'EventSourceType') AND ([collation_name] <> N'Latin1_General_100_BIN2' OR [max_length] <> 512))");

    [Fact] void should_keep_widening_and_backfilling_in_one_transaction() => _operation.SuppressTransaction.ShouldBeFalse();
    [Fact] void should_drop_the_primary_key_first() => _statements[2].ShouldEqual("ALTER TABLE [ClosedStreams] DROP CONSTRAINT [PK_ClosedStreams];");

    [Fact]
    void should_make_every_key_column_exact() =>
        _columns.Select(column => _statements.Count(_ => _ == $"ALTER TABLE [ClosedStreams] ALTER COLUMN [{column}] nvarchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL;")).ShouldEqual([1, 1, 1, 1]);

    [Fact]
    void should_record_each_columns_encoding_state_before_widening() =>
        _columns.Select(column => _statements[IndexOf($"DECLARE @Encode{column}")]).ShouldEqual(
            _columns.Select(column => $"DECLARE @Encode{column} bit = CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[ClosedStreams]') AND [name] = N'{column}' AND [max_length] <> 512) THEN 1 ELSE 0 END;"));

    [Fact]
    void should_read_legacy_widths_before_changing_any_column() =>
        IndexOf("ALTER TABLE [ClosedStreams] ALTER COLUMN").ShouldBeGreaterThan(IndexOf("DECLARE @EncodeEventSourceType"));

    [Fact]
    void should_encode_legacy_values_without_encoding_already_widened_columns_again() =>
        _columns.Select(column => _statements[IndexOf("UPDATE")].Contains($"[{column}] = CASE WHEN @Encode{column} = 1 AND UNICODE(RIGHT([{column}], 1)) IN (32, 127) THEN [{column}] + NCHAR(127) ELSE [{column}] END", StringComparison.Ordinal)).ShouldEqual([true, true, true, true]);

    [Fact]
    void should_only_backfill_legacy_columns_with_trailing_spaces_or_terminators() =>
        _statements[IndexOf("UPDATE")].ShouldContain("WHERE @EncodeStreamType = 1 AND UNICODE(RIGHT([StreamType], 1)) IN (32, 127) OR @EncodeStreamId = 1 AND UNICODE(RIGHT([StreamId], 1)) IN (32, 127) OR @EncodeEventSourceId = 1 AND UNICODE(RIGHT([EventSourceId], 1)) IN (32, 127) OR @EncodeEventSourceType = 1 AND UNICODE(RIGHT([EventSourceType], 1)) IN (32, 127);");
    [Fact] void should_terminate_after_changing_the_columns() => IndexOf("UPDATE").ShouldBeGreaterThan(IndexOf("ALTER TABLE [ClosedStreams] ALTER COLUMN [EventSourceType]"));
    [Fact] void should_create_the_primary_key_after_terminating() => IndexOf("ALTER TABLE [ClosedStreams] ADD CONSTRAINT").ShouldBeGreaterThan(IndexOf("UPDATE"));

    [Fact]
    void should_create_the_same_primary_key_again() =>
        _statements[IndexOf("ALTER TABLE [ClosedStreams] ADD CONSTRAINT")].ShouldEqual("ALTER TABLE [ClosedStreams] ADD CONSTRAINT [PK_ClosedStreams] PRIMARY KEY ([EventSequenceId], [StreamType], [StreamId], [EventSourceId], [EventSourceType], [Owner], [Dimensions]);");
}
