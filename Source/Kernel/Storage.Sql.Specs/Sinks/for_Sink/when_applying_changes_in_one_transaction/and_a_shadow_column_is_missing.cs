// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The database reports a missing shadow column while the cycle writes: the cycle repairs the table and reruns whole, applying once.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_shadow_column_is_missing<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J"), ("x", 1)));
        _capture.Clear();
        _capture.ThrowBeforeCommand(IsReadModelInsert, new SqliteException("no such column: __compositeKey", 1));
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
    }

    [Fact] public void should_succeed() => _error.ShouldBeNull();
    [Fact] public void should_apply_once() => ChildCount(_row).ShouldEqual(1);
    [Fact] public void should_rerun_in_a_new_transaction() => _capture.Started.ShouldEqual(2);
}
