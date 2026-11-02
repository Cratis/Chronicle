// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A competing writer inserts the same row just before the cycle does: the cycle rolls back and reruns, finding the row and updating it.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_an_insert_race_happens_inside_the_cycle<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    /// <summary>
    /// Inserts the same row as the cycle is about to. SQLite cannot take a second connection while the cycle holds the write lock.
    /// </summary>
    protected virtual bool CompetesOnTheCyclesConnection => false;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
        _capture.BeforeCommand(IsReadModelInsert, async command =>
        {
            if (CompetesOnTheCyclesConnection)
            {
                await ExecuteOn(command, $"INSERT INTO {Quote(TableName)} ({Quote("id")}) VALUES ('racer')");
            }
            else
            {
                await ApplyOn(AnotherSink(), "racer", 3, Set(("x", 99)));
            }
        });
    }

    async Task Because()
    {
        try
        {
            await Apply("racer", 4, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _row = await Row("racer");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_apply_this_cycle() => _row!["group"].ShouldEqual("direct");
    [Fact] public void should_rerun_in_a_new_transaction() => _capture.Started.ShouldEqual(2);
    [Fact] public void should_keep_the_competing_write() => (CompetesOnTheCyclesConnection ? 99 : Convert.ToInt32(_row!["x"])).ShouldEqual(99);
}
