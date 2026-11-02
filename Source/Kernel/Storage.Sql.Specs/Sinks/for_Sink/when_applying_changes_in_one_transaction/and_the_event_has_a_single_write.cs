// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public abstract class and_the_event_has_a_single_write<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    async Task Establish()
    {
        await Apply("source", 1, Set(("group", "first")));
        _capture.Clear();
    }

    Task Because() => Apply("source", 2, Set(("group", "second")));

    [Fact] public void should_not_begin_a_transaction_of_its_own() => _capture.Started.ShouldEqual(0);
    [Fact] public void should_not_touch_the_receipt_table() => ReceiptCommands.ShouldBeEmpty();
    [Fact] public void should_not_create_savepoints() => _capture.Savepoints.ShouldEqual(0);
}
