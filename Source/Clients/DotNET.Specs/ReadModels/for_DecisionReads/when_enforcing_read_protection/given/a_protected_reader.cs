// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection.given;

public class a_protected_reader : for_DecisionReads.given.a_decision_reader
{
    protected UnitOfWork _unit;
    protected IUnitOfWorkManager _manager;

    protected record Created;

    void Establish()
    {
        _unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        _manager = Substitute.For<IUnitOfWorkManager>();
        _manager.HasCurrent.Returns(true);
        _manager.Current.Returns(_unit);
        _store.UnitOfWorkManager.Returns(_manager);
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(typeof(Created)).Returns(new EventType("created", 1));
        _store.EventTypes.Returns(eventTypes);
    }
}
