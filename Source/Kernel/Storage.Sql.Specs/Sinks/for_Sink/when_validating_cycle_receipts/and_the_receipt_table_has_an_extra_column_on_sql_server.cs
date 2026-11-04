// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

[Collection(SqlServerCollection.Name)]
public class and_the_receipt_table_has_an_extra_column_on_sql_server(SqlServerFixture fixture) : and_the_receipt_table_has_an_extra_column<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture], ConfigureOptions = Configure };

    protected override IReadOnlyList<string> NullableColumn => ["ALTER TABLE [chronicle_sink_cycle_receipts] ADD [Extra] int NULL"];

    protected override IReadOnlyList<string> RequiredColumnWithDefault => ["ALTER TABLE [chronicle_sink_cycle_receipts] ADD [Extra] int NOT NULL DEFAULT 0"];

    protected override IReadOnlyList<string> RequiredColumnWithoutDefault => ["ALTER TABLE [chronicle_sink_cycle_receipts] ADD [Extra] int NOT NULL"];

    protected override IReadOnlyList<string> GeneratedColumn => ["ALTER TABLE [chronicle_sink_cycle_receipts] ADD [Extra] AS (1) PERSISTED NOT NULL"];
}
