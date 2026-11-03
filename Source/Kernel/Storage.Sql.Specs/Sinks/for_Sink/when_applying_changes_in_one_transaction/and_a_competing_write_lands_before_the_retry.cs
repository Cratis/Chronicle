// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A competing write commits between the lost commit and the retry: the retry must skip and leave the competing write alone.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_competing_write_lands_before_the_retry<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;
    long _watermark;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
        _capture.AfterNextCommit(() => ApplyOn(AnotherSink(), "row", 9, Set(("x", 2))), true);
    }

    async Task Because()
    {
        try
        {
            await Apply("row", 5, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("x", 1)));
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _row = await Row("row");
        _watermark = await Watermark("row");
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_keep_the_competing_value() => Convert.ToInt32(_row!["x"]).ShouldEqual(2);
    [Fact] public void should_keep_the_competing_watermark() => _watermark.ShouldEqual(9);
}
