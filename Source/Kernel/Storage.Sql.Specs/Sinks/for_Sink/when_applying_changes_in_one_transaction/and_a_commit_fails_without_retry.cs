// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// Without a retrying execution strategy a failed commit surfaces to the caller and nothing is stored.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_commit_fails_without_retry<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;
    IDictionary<string, object?>? _target;

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
            await Apply("row", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _row = await Row("row");
        _target = await Row("target");
    }

    [Fact] public void should_fail() => _error.ShouldNotBeNull();
    [Fact] public void should_store_no_row() => _row.ShouldBeNull();
    [Fact] public void should_not_apply_the_join() => Convert.ToInt32(_target!["x"]).ShouldEqual(1);
}
