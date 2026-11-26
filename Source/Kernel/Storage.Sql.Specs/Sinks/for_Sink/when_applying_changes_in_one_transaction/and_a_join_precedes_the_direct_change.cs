// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// A Join comes before a direct change on the same property of the same row; the direct change comes last, so its value wins.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
[PerFactSpecificationLifecycle] // Facts run their own asynchronous operations, so each fact gets its own context.
public abstract class and_a_join_precedes_the_direct_change<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    protected abstract SinkWriteMode Mode { get; }

    async Task Establish() => await Apply("root", 1, Set(("joinId", "root"), ("x", 0), ("group", "seed")));

    async Task Because()
    {
        await Apply("root", 2, Mode, KeyedFromJoin("joinId", "root", ("x", 2)), Set(("x", 1)));
        _row = await Row("root");
    }

    [Fact] public void should_store_the_direct_value() => Convert.ToInt32(_row!["x"]).ShouldEqual(1);
}
