// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_several_writes<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    async Task Establish()
    {
        await Apply("target", 1, Set(("joinId", "J")));
        _capture.Clear();
    }

    Task Because() => Apply("source", 2, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));

    [Fact] public void should_begin_one_transaction() => _capture.Started.ShouldEqual(1);
    [Fact] public void should_commit_once() => _capture.Committed.ShouldEqual(1);
    [Fact] public void should_not_create_savepoints() => _capture.Savepoints.ShouldEqual(0);
    [Fact] public void should_insert_one_receipt() => ReceiptInserts.Count().ShouldEqual(1);
}
