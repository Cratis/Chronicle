// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    [Fact] public void should_begin_one_transaction() => _capture.Started.ShouldEqual(1);
    [Fact] public void should_commit_once() => _capture.Committed.ShouldEqual(1);
    [Fact] public void should_apply_the_join() => Convert.ToInt32(_target!["x"]).ShouldEqual(7);
    [Fact] public void should_apply_the_direct_changes() => _source!["group"].ShouldEqual("direct");
    [Fact] public async Task should_remove_the_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
