// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager.given;

public class a_unit_of_work_manager : Specification
{
    protected IEventStore _eventStore;
    protected UnitOfWorkManager _manager;


    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _manager = new UnitOfWorkManager(_eventStore);
    }
}
