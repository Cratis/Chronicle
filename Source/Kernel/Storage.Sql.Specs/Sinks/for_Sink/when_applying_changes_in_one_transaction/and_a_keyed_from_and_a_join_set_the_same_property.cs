// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A keyed From and a Join of one event set the same property of the same row; the Join comes after the From, so its value wins.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
[PerFactSpecificationLifecycle] // Facts run their own asynchronous operations, so each fact gets its own context.
public abstract class and_a_keyed_from_and_a_join_set_the_same_property<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    protected abstract SinkWriteMode Mode { get; }

    async Task Establish() => await Apply("root", 1, Set(("joinId", "root"), ("x", 0), ("group", "seed")));

    async Task Because()
    {
        await Apply("root", 2, Mode, Set(("x", 1), ("group", "direct")), KeyedFromJoin("joinId", "root", ("x", 2)));
        _row = await Row("root");
    }

    [Fact] public void should_store_the_value_the_join_computed() => Convert.ToInt32(_row!["x"]).ShouldEqual(2);
    [Fact] public void should_store_the_direct_property_the_join_did_not_touch() => _row!["group"].ShouldEqual("direct");
}
