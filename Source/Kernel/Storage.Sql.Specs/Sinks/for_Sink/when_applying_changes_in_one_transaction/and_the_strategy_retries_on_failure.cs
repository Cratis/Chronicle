// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A strategy that retries, however the database is reached, needs the receipt: the cycle writes one and a retry probes it.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_strategy_retries_on_failure<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        _capture.Clear();
        _capture.FailNextCommitBeforeSending();
    }

    async Task Because()
    {
        await Apply("row", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        _row = await Row("row");
    }

    [Fact] public void should_write_the_receipt_in_each_attempt() => ReceiptInserts.Count().ShouldEqual(2);
    [Fact] public void should_probe_the_receipt_in_the_retry() => ReceiptProbes.Count().ShouldEqual(1);
    [Fact] public void should_apply_the_cycle_once() => _row.ShouldNotBeNull();
    [Fact] public async Task should_remove_the_receipt_when_done() => (await ReceiptCount()).ShouldEqual(0);
}
