// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// Two joins that chain: J1 moves group A to B and J2 moves C to A. Applying the cycle twice would also move the rows J1 just produced, so the retry must skip it.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_commit_acknowledgment_is_lost_after_chained_joins<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _first;
    IDictionary<string, object?>? _second;

    async Task Establish()
    {
        await Apply("r1", 1, Set(("group", "A")));
        await Apply("r2", 2, Set(("group", "C")));
        _capture.Clear();
        _capture.AfterNextCommit(() => Task.CompletedTask, true);
    }

    async Task Because()
    {
        try
        {
            await Apply("trigger", 3, KeyedFromJoin("group", "A", ("group", "B")), KeyedFromJoin("group", "C", ("group", "A")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _first = await Row("r1");
        _second = await Row("r2");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_move_the_first_row_to_b() => _first!["group"].ShouldEqual("B");
    [Fact] public void should_move_the_second_row_to_a() => _second!["group"].ShouldEqual("A");
    [Fact] public void should_write_each_join_once() => ReadModelWrites.Where(IsReadModelUpdate).Sum(command => command.Split("UPDATE ", StringSplitOptions.None).Length - 1).ShouldEqual(2);
}
