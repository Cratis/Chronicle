// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A join and a direct write in one cycle: when the direct write fails, the join's write must not stay behind.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_later_write_fails<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _joined;
    IDictionary<string, object?>? _direct;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
        _capture.ThrowBeforeCommand(command => command.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase) && !command.Contains("receipts", StringComparison.Ordinal), new InvalidOperationException("injected"));
    }

    async Task Because()
    {
        try
        {
            await Apply("source", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _joined = await Row("target");
        _direct = await Row("source");
    }

    [Fact] public void should_fail() => _error.ShouldNotBeNull();
    [Fact] public void should_not_store_the_join_changes() => Convert.ToInt32(_joined!["x"]).ShouldEqual(0);
    [Fact] public void should_not_store_the_direct_changes() => _direct.ShouldBeNull();
    [Fact] public async Task should_not_store_a_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
