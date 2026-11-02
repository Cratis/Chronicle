// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

public class and_the_receipt_table_has_an_extra_column_on_sqlite : and_the_receipt_table_has_an_extra_column<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture], ConfigureOptions = Configure };

    protected override IReadOnlyList<string> NullableColumn => ["ALTER TABLE \"chronicle_sink_cycle_receipts\" ADD COLUMN \"Extra\" INTEGER"];

    protected override IReadOnlyList<string> RequiredColumnWithDefault => ["ALTER TABLE \"chronicle_sink_cycle_receipts\" ADD COLUMN \"Extra\" INTEGER NOT NULL DEFAULT 0"];

    /// <summary>
    /// SQLite cannot add a required column without a default, so the table is created again with one.
    /// </summary>
    protected override IReadOnlyList<string> RequiredColumnWithoutDefault =>
    [
        "DROP TABLE \"chronicle_sink_cycle_receipts\"",
        "CREATE TABLE \"chronicle_sink_cycle_receipts\" (\"Id\" TEXT NOT NULL, \"TableName\" TEXT NOT NULL, \"EventSequenceNumber\" INTEGER NOT NULL, \"CreatedAt\" INTEGER NOT NULL DEFAULT (CAST(strftime('%s', 'now') AS INTEGER) * 1000), \"Extra\" INTEGER NOT NULL, CONSTRAINT \"PK_chronicle_sink_cycle_receipts\" PRIMARY KEY (\"Id\"))",
        "CREATE INDEX \"IX_chronicle_sink_cycle_receipts_CreatedAt\" ON \"chronicle_sink_cycle_receipts\" (\"CreatedAt\")"
    ];

    protected override IReadOnlyList<string> GeneratedColumn => ["ALTER TABLE \"chronicle_sink_cycle_receipts\" ADD COLUMN \"Extra\" INTEGER GENERATED ALWAYS AS (1) VIRTUAL"];
}
