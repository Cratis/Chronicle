// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The server commits a multi-write cycle but the caller never hears: the retry must recognise its own receipt
/// and not apply the cycle a second time.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_commit_acknowledgment_is_lost<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;
    IDictionary<string, object?>? _target;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        _capture.Clear();
        _capture.AfterNextCommit(() => Task.CompletedTask, true);
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
        _target = await Row("target");
    }

    [Fact] public void should_lose_the_acknowledgment() => _capture.LostAcknowledgments.ShouldEqual(1);
    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_store_the_children_once() => ChildCount(_row).ShouldEqual(1);
    [Fact] public void should_store_the_join_value() => Convert.ToInt32(_target!["x"]).ShouldEqual(7);
    [Fact] public void should_issue_no_write_after_the_lost_commit() => _capture.WriteCommands.Count(IsReadModelInsert).ShouldEqual(1);
}
