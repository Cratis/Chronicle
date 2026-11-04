// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// Finished receipts are deleted in batches, so some are still there when the next cycle runs. They are never read as an answer:
/// a later cycle, even one with the same sequence number, applies normally.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_finished_receipts_are_left_uncleaned<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    int _leftovers;
    IDictionary<string, object?>? _first;
    IDictionary<string, object?>? _second;

    protected override int ReceiptCleanupBatchSize => 64;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 0)));
        await Apply("warm", 4, KeyedFromJoin("joinId", "J", ("x", 0)), Set(("group", "g0")));
        await Apply("first", 5, KeyedFromJoin("joinId", "J", ("x", 1)), Set(("group", "g1")));
        _leftovers = await ReceiptCount();
        _capture.Clear();
    }

    async Task Because()
    {
        await Apply("second", 5, KeyedFromJoin("joinId", "J", ("x", 2)), Set(("group", "g2")));
        _first = await Row("first");
        _second = await Row("second");
    }

    [Fact] public void should_have_left_receipts_behind() => _leftovers.ShouldBeGreaterThan(0);
    [Fact] public void should_apply_the_later_cycle() => _second.ShouldNotBeNull();
    [Fact] public void should_store_the_later_direct_change() => _second!["group"].ShouldEqual("g2");
    [Fact] public void should_keep_the_earlier_row() => _first!["group"].ShouldEqual("g1");
    [Fact] public void should_not_probe_a_leftover_receipt() => ReceiptProbes.ShouldBeEmpty();
}
