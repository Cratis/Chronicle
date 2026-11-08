// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

public class when_upgrading_for_sql_server : Specification
{
    static readonly string[] _columns = ["StreamType", "StreamId", "EventSourceId", "EventSourceType"];
    string[] _statements;

    void Because() => _statements = new v20_0_2 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.UpOperations
        .Cast<SqlOperation>()
        .Single()
        .Sql.Split('\n');

    int IndexOf(string start) => Array.FindIndex(_statements, _ => _.StartsWith(start, StringComparison.Ordinal));

    [Fact] void should_skip_the_change_when_the_columns_are_already_exact() => _statements[0].ShouldContain("[collation_name] <> N'Latin1_General_100_BIN2'");
    [Fact] void should_drop_the_primary_key_first() => _statements[2].ShouldEqual("ALTER TABLE [ClosedStreams] DROP CONSTRAINT [PK_ClosedStreams];");

    [Fact]
    void should_make_every_key_column_exact() =>
        _columns.Select(column => _statements.Count(_ => _ == $"ALTER TABLE [ClosedStreams] ALTER COLUMN [{column}] nvarchar(256) COLLATE Latin1_General_100_BIN2 NOT NULL;")).ShouldEqual([1, 1, 1, 1]);

    [Fact] void should_terminate_values_that_end_with_a_space_or_the_terminator() => _statements[IndexOf("UPDATE")].ShouldContain("[StreamId] = CASE WHEN UNICODE(RIGHT([StreamId], 1)) IN (32, 1) THEN [StreamId] + NCHAR(1) ELSE [StreamId] END");
    [Fact] void should_terminate_after_changing_the_columns() => IndexOf("UPDATE").ShouldBeGreaterThan(IndexOf("ALTER TABLE [ClosedStreams] ALTER COLUMN [EventSourceType]"));
    [Fact] void should_create_the_primary_key_after_terminating() => IndexOf("ALTER TABLE [ClosedStreams] ADD CONSTRAINT").ShouldBeGreaterThan(IndexOf("UPDATE"));

    [Fact]
    void should_create_the_same_primary_key_again() =>
        _statements[IndexOf("ALTER TABLE [ClosedStreams] ADD CONSTRAINT")].ShouldEqual("ALTER TABLE [ClosedStreams] ADD CONSTRAINT [PK_ClosedStreams] PRIMARY KEY ([EventSequenceId], [StreamType], [StreamId], [EventSourceId], [EventSourceType], [Owner], [Dimensions]);");
}
