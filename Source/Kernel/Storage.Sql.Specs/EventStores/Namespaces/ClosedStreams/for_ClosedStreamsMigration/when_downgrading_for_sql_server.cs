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

    [Fact] void should_only_run_when_the_columns_are_exact() => _statements[0].ShouldContain("[collation_name] = N'Latin1_General_100_BIN2'");
    [Fact] void should_strip_one_terminator() => _statements[IndexOf("UPDATE")].ShouldContain("[StreamId] = CASE WHEN UNICODE(RIGHT([StreamId], 1)) = 1 THEN LEFT([StreamId], DATALENGTH([StreamId]) / 2 - 1) ELSE [StreamId] END");
    [Fact] void should_strip_before_restoring_the_collation() => IndexOf("ALTER TABLE [ClosedStreams] ALTER COLUMN").ShouldBeGreaterThan(IndexOf("UPDATE"));
    [Fact] void should_restore_the_database_default_collation() => _statements.ShouldContain("ALTER TABLE [ClosedStreams] ALTER COLUMN [StreamId] nvarchar(255) COLLATE DATABASE_DEFAULT NOT NULL;");
    [Fact] void should_create_the_primary_key_last() => _statements[^2].StartsWith("ALTER TABLE [ClosedStreams] ADD CONSTRAINT [PK_ClosedStreams] PRIMARY KEY", StringComparison.Ordinal).ShouldBeTrue();
}
