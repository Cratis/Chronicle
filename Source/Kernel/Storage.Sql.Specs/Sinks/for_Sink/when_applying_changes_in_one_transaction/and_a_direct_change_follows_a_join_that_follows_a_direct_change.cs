// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A direct change, a Join and a later direct change all set one property of the same row; the last change in changeset order wins.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
[PerFactSpecificationLifecycle] // Facts run their own asynchronous operations, so each fact gets its own context.
public abstract class and_a_direct_change_follows_a_join_that_follows_a_direct_change<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    protected abstract SinkWriteMode Mode { get; }

    async Task Establish() => await Apply("root", 1, Set(("joinId", "root"), ("x", 0), ("group", "seed")));

    async Task Because()
    {
        await Apply("root", 2, Mode, Set(("group", "a")), KeyedFromJoin("joinId", "root", ("group", "b")), Set(("group", "c")));
        _row = await Row("root");
    }

    [Fact] public void should_store_the_value_of_the_last_change() => _row!["group"].ShouldEqual("c");
}
