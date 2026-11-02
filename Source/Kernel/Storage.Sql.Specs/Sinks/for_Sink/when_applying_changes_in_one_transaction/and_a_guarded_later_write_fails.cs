// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A join and a guarded direct write in one cycle: when the guarded update fails, the join's write must not stay behind and nothing is committed.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_guarded_later_write_fails<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _target;
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        await Apply("row", 2, Set(("x", 1)));
        _capture.Clear();
        _capture.ThrowBeforeCommand(command => IsReadModelUpdate(command) && command.Contains("lastHandled", StringComparison.OrdinalIgnoreCase) && !command.Contains(Quote("joinId"), StringComparison.Ordinal), new InvalidOperationException("injected"));
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

    [Fact] public void should_fail() => _error.ShouldNotBeNull();
    [Fact] public void should_not_store_the_join_changes() => Convert.ToInt32(_target!["x"]).ShouldEqual(1);
    [Fact] public void should_not_store_the_direct_changes() => _row!.ContainsKey("group").ShouldBeFalse();
    [Fact] public void should_not_commit() => _capture.Committed.ShouldEqual(0);
    [Fact] public async Task should_not_store_a_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
