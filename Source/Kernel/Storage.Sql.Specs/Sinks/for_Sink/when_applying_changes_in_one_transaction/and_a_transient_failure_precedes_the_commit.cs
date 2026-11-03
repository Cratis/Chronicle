// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The commit never reaches the server: the retry must probe its receipt, find none, and apply the cycle once.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_transient_failure_precedes_the_commit<THarness> : given.a_cycle_with_failure_injection<THarness>
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

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_retry_once() => _capture.Started.ShouldEqual(2);
    [Fact] public void should_store_the_children_once() => ChildCount(_row).ShouldEqual(1);
    [Fact] public void should_probe_the_receipt_first_in_the_retry() => ReceiptProbes.Count().ShouldEqual(1);
    [Fact] public async Task should_remove_the_receipt_when_done() => (await ReceiptCount()).ShouldEqual(0);
}
