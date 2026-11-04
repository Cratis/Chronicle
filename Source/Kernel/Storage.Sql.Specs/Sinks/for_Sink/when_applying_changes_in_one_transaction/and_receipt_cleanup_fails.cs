// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_receipt_cleanup_fails : given.a_cycle_with_failure_injection<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture], ConfigureOptions = given.retrying_on_timeout.Configure };

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
        _capture.ThrowBeforeCommand(command => command.Contains("DELETE", StringComparison.OrdinalIgnoreCase) && command.Contains(ReceiptTable, StringComparison.Ordinal), new InvalidOperationException("cleanup unavailable"));
    }

    async Task Because() => _error = await Catch.Exception(async () => await Apply("source", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "new"))));

    [Fact] public void should_not_change_the_successful_outcome() => _error.ShouldBeNull();
    [Fact] public void should_commit_once() => _capture.Committed.ShouldEqual(1);
    [Fact] public async Task should_keep_the_join() => (await Watermark("target")).ShouldEqual(2);
    [Fact] public async Task should_keep_the_direct_write() => (await Watermark("source")).ShouldEqual(2);
    [Fact] public async Task should_leave_the_receipt_for_a_later_sweep() => (await ReceiptCount()).ShouldEqual(1);
}
