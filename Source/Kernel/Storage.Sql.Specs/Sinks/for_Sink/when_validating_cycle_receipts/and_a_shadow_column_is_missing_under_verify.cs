// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// The database reports a missing shadow column during a Verify multi-write cycle: the cycle repairs the read-model table
/// and reruns whole, exactly as single writes and Migrate cycles do. Only the receipt table is subject to fail-closed Verify.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_shadow_column_is_missing_under_verify<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _result;
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
        _verify = true;
        _capture.Clear();
        Exception missingColumn = _harness.ConnectionString.GetDatabaseType() == DatabaseType.PostgreSql
            ? new PostgresException("column does not exist", "ERROR", "ERROR", PostgresErrorCodes.UndefinedColumn)
            : new SqliteException("no such column: __compositeKey", 1);
        _capture.ThrowBeforeCommand(IsReadModelInsert, missingColumn);
    }

    async Task Because()
    {
        _result = await Attempt(AnotherSink(), "row", 3);
        _row = await Row("row");
    }

    [Fact] public void should_succeed() => _result.ShouldBeNull();
    [Fact] public void should_write_the_row() => _row.ShouldNotBeNull();
    [Fact] public void should_rerun_in_a_new_transaction() => _capture.Started.ShouldEqual(2);
    [Fact] public void should_not_touch_the_receipt_ddl() => ReceiptDdl.ShouldBeEmpty();
}
