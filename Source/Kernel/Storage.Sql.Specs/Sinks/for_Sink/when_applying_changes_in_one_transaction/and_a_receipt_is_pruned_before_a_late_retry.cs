// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The cycle committed, the receipt was swept before the late retry looked: past the window the retry fails closed instead of applying the cycle a second time.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_receipt_is_pruned_before_a_late_retry<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        ((Sink)_sink).ReceiptVerificationWindow = TimeSpan.FromMilliseconds(-1);
        _capture.Clear();
        _capture.AfterNextCommit(() => Execute($"DELETE FROM {Quote(ReceiptTable)}"), true);
    }

    async Task Because()
    {
        try
        {
            await Apply("row", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")), AddChild("c1"));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _row = await Row("row");
    }

    [Fact] public void should_fail_closed() => _error.ShouldBeOfExactType<SinkCycleOutcomeUnknown>();
    [Fact] public void should_keep_a_single_application() => ChildCount(_row).ShouldEqual(1);
    [Fact] public void should_not_write_again() => _capture.WriteCommands.Count(IsReadModelInsert).ShouldEqual(1);
}
