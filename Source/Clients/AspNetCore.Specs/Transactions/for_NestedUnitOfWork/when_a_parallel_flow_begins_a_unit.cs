// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_NestedUnitOfWork;

public class when_a_parallel_flow_begins_a_unit : Specification
{
    UnitOfWorkManager _manager;
    bool _parentStillCurrent;

    void Establish() => _manager = new UnitOfWorkManager(Substitute.For<IEventStore>());

    async Task Because()
    {
        var request = _manager.Begin(CorrelationId.New());
        await Task.Run(() => _manager.Begin(CorrelationId.New()));
        _parentStillCurrent = ReferenceEquals(_manager.Current, request);
    }

    [Fact] void should_leave_the_parent_flow_current_unit_alone() => _parentStillCurrent.ShouldBeTrue();
}
