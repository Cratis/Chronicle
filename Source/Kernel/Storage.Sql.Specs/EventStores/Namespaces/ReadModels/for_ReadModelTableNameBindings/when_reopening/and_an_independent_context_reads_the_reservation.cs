// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_reopening;

public class and_an_independent_context_reads_the_reservation : given.a_sqlite_ledger
{
    ReadModelTableNameBinding _stored;

    async Task Establish() => await Reserve(_owner, "orders");

    async Task Because()
    {
        var ledger = CreateLedger(CreateContext());
        _stored = await ledger.UnderLock(async () => (await ledger.Find(_owner.Id))!);
    }

    [Fact] void should_keep_the_full_scoped_owner() => _stored.Owner.ShouldEqual(_owner);
    [Fact] void should_keep_the_physical_name() => _stored.PhysicalName.ShouldEqual("orders");
    [Fact] void should_keep_the_reservation_state() => _stored.State.ShouldEqual(ReadModelTableBindingState.Reserved);
    [Fact] void should_not_invent_a_replay_attempt() => _stored.ReplayAttempt.ShouldBeNull();
}
