// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A guarded cycle with a join and a direct write commits both in one transaction, with one receipt written and removed.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_guarded_cycle_has_several_writes<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _target;
    IDictionary<string, object?>? _source;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
    }

    async Task Because()
    {
        await Apply("source", 2, SinkWriteMode.GuardedUpsert, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        _target = await Row("target");
        _source = await Row("source");
    }

    [Fact]
    public void should_not_flush_the_receipt_before_reading_the_join_candidates()
    {
        var commands = _capture.Commands.ToList();
        var firstReceipt = commands.FindIndex(command => command.Contains(ReceiptTable, StringComparison.Ordinal) && command.Contains("INSERT", StringComparison.OrdinalIgnoreCase));
        var firstRead = commands.FindIndex(command => command.Contains("SELECT", StringComparison.OrdinalIgnoreCase) && command.Contains(TableName, StringComparison.Ordinal));
        firstReceipt.ShouldBeGreaterThan(firstRead);
    }

    [Fact]
    public void should_batch_the_receipt_with_the_first_postgresql_row_write()
    {
        if (_harness.ConnectionString.GetDatabaseType() == DatabaseType.PostgreSql)
        {
            ReceiptInserts.Single().ShouldEqual(ReadModelWrites.First());
        }
    }

    [Fact] public void should_insert_one_receipt() => _capture.Commands.Sum(command => SqlCommandCapture.CountStatements(command, "INSERT INTO", ReceiptTable)).ShouldEqual(1);
    [Fact] public void should_update_the_join_row_once() => _capture.Commands.Sum(command => SqlCommandCapture.CountStatements(command, "UPDATE", TableName)).ShouldEqual(1);
    [Fact] public void should_insert_the_direct_row_once() => _capture.Commands.Sum(command => SqlCommandCapture.CountStatements(command, "INSERT INTO", TableName)).ShouldEqual(1);
    [Fact] public void should_not_create_savepoints() => _capture.Savepoints.ShouldEqual(0);
    [Fact] public void should_not_probe_before_the_body() => ReceiptProbes.ShouldBeEmpty();
    [Fact] public async Task should_advance_the_join_watermark() => (await Watermark("target")).ShouldEqual(2);
    [Fact] public async Task should_advance_the_direct_watermark() => (await Watermark("source")).ShouldEqual(2);
    [Fact] public void should_begin_one_transaction() => _capture.Started.ShouldEqual(1);
    [Fact] public void should_commit_once() => _capture.Committed.ShouldEqual(1);
    [Fact] public void should_apply_the_join() => Convert.ToInt32(_target!["x"]).ShouldEqual(7);
    [Fact] public void should_apply_the_direct_changes() => _source!["group"].ShouldEqual("direct");
    [Fact] public async Task should_remove_the_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
