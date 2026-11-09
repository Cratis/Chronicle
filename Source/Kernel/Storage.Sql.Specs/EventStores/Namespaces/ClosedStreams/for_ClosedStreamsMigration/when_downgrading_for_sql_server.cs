// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

public class when_downgrading_for_sql_server : Specification
{
    string[] _statements;

    void Because() => _statements = new v20_0_2 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.DownOperations
        .Cast<SqlOperation>()
        .Single()
        .Sql.Split('\n');

    int IndexOf(string start) => Array.FindIndex(_statements, _ => _.StartsWith(start, StringComparison.Ordinal));

    [Fact]
    void should_skip_already_downgraded_columns_even_when_the_database_default_is_binary() =>
        _statements[0].ShouldEqual("IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[ClosedStreams]') AND [name] IN (N'StreamType', N'StreamId', N'EventSourceId', N'EventSourceType') AND [max_length] = 512)");

    [Fact]
    void should_only_decode_columns_that_were_widened() =>
        _statements.ShouldContain("DECLARE @EncodeStreamId bit = CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[ClosedStreams]') AND [name] = N'StreamId' AND [max_length] = 512) THEN 1 ELSE 0 END;");

    [Fact] void should_strip_one_terminator() => _statements[IndexOf("UPDATE")].ShouldContain("[StreamId] = CASE WHEN @EncodeStreamId = 1 AND UNICODE(RIGHT([StreamId], 1)) = 1 THEN LEFT([StreamId], DATALENGTH([StreamId]) / 2 - 1) ELSE [StreamId] END");
    [Fact] void should_strip_before_restoring_the_collation() => IndexOf("ALTER TABLE [ClosedStreams] ALTER COLUMN").ShouldBeGreaterThan(IndexOf("UPDATE"));
    [Fact] void should_restore_the_database_default_collation() => _statements.ShouldContain("ALTER TABLE [ClosedStreams] ALTER COLUMN [StreamId] nvarchar(255) COLLATE DATABASE_DEFAULT NOT NULL;");
    [Fact] void should_create_the_primary_key_last() => _statements[^2].StartsWith("ALTER TABLE [ClosedStreams] ADD CONSTRAINT [PK_ClosedStreams] PRIMARY KEY", StringComparison.Ordinal).ShouldBeTrue();
}
