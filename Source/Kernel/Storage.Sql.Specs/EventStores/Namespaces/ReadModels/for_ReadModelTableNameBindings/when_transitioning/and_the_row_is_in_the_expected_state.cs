// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_transitioning;

public class and_the_row_is_in_the_expected_state : given.a_sqlite_ledger
{
    bool _moved;
    bool _movedAgain;
    ReadModelTableNameBinding _stored;

    async Task Establish() => await Reserve(_owner, "orders");

    async Task Because()
    {
        _moved = await _ledger.Transition(_owner.Id, ReadModelTableBindingState.Reserved, ReadModelTableBindingState.Ready);
        _movedAgain = await _ledger.Transition(_owner.Id, ReadModelTableBindingState.Reserved, ReadModelTableBindingState.Removed);
        _stored = await CreateLedger(CreateContext()).Find(_owner.Id);
    }

    [Fact] void should_move_the_row() => _moved.ShouldBeTrue();
    [Fact] void should_refuse_a_stale_expectation() => _movedAgain.ShouldBeFalse();
    [Fact] void should_keep_the_first_committed_state() => _stored.State.ShouldEqual(ReadModelTableBindingState.Ready);
    [Fact] void should_keep_the_physical_name() => _stored.PhysicalName.ShouldEqual("orders");
}
