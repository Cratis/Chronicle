// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// An existing-row CAS clears row tracking before any join save; the pending receipt still reaches the fallback save.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_guarded_cycle_has_no_join_candidates<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("source", 1, Set(("x", 1)));
        _capture.Clear();
        _capture.AfterNextCommit(() => Task.CompletedTask, true);
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => await Apply("source", 2, SinkWriteMode.GuardedUpsert, KeyedFromJoin("joinId", "absent", ("x", 7)), AddChild("c1")));
        _row = await Row("source");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_insert_one_receipt() => ReceiptInserts.Count().ShouldEqual(1);
    [Fact] public void should_probe_after_the_lost_acknowledgment() => ReceiptProbes.Count().ShouldEqual(1);
    [Fact] public void should_not_duplicate_the_child() => ChildCount(_row).ShouldEqual(1);
    [Fact] public async Task should_keep_the_watermark() => (await Watermark("source")).ShouldEqual(2);
    [Fact] public async Task should_remove_the_receipt() => (await ReceiptCount()).ShouldEqual(0);
}
