// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_of_work : Specification
{
    protected IEventStore _eventStore;
    protected UnitOfWork _unitOfWork;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _correlationId = CorrelationId.New();
        _unitOfWork = new(_correlationId, OnUnitOfWorkCompleted, _eventStore);
    }

    protected virtual void OnUnitOfWorkCompleted(IUnitOfWork unitOfWork)
    {
    }
}
