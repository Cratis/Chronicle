// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A competing write moves the watermark between the cycle's read and its guarded update: the cycle reruns whole, applying the join once and the recomputed direct changes.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_guarded_write_loses_its_compare_and_swap<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _target;
    IDictionary<string, object?>? _row;

    protected virtual bool CompetesOnTheCyclesConnection => false;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        await Apply("row", 2, Set(("x", 1)));
        _capture.Clear();
        _capture.BeforeCommand(command => IsReadModelUpdate(command) && command.Contains("lastHandled", StringComparison.OrdinalIgnoreCase) && !command.Contains(Quote("joinId"), StringComparison.Ordinal), async command =>
        {
            if (CompetesOnTheCyclesConnection)
            {
                await ExecuteOn(command, $"UPDATE {Quote(TableName)} SET {Quote("__lastHandledEventSequenceNumber")} = 7 WHERE {Quote("id")} = 'row'");
            }
            else
            {
                await ApplyOn(AnotherSink(), "row", 7, Set(("x", 3)));
            }
        });
    }

    async Task Because()
    {
        try
        {
            await Apply("row", 9, SinkWriteMode.GuardedUpsert, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _target = await Row("target");
        _row = await Row("row");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_apply_the_join_once() => Convert.ToInt32(_target!["x"]).ShouldEqual(7);
    [Fact] public void should_apply_the_recomputed_direct_changes() => _row!["group"].ShouldEqual("direct");
    [Fact] public async Task should_leave_no_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
