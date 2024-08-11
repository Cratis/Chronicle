// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class a_stateful_aggregate_root : all_dependencies
{
    protected StatefulAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IAggregateRootContext _aggregateRootContext;
    protected IUnitOfWork _unitOfWork;

    void Establish()
    {
        _aggregateRoot = new();

        _eventSourceId = Guid.NewGuid().ToString();

        _unitOfWork = Substitute.For<IUnitOfWork>();

        _aggregateRootContext = new AggregateRootContext(_eventSourceId, _eventSequence, _aggregateRoot, _unitOfWork);
        _aggregateRoot._context = _aggregateRootContext;
        _aggregateRoot._mutation = _mutation;
    }
}
