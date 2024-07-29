// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class a_stateful_aggregate_root : all_dependencies
{
    protected StatefulAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IAggregateRootContext _aggregateRootContext;

    void Establish()
    {
        _aggregateRoot = new();

        _eventSourceId = Guid.NewGuid().ToString();

        _aggregateRootContext = new AggregateRootContext(CorrelationId.New(), _eventSourceId, _eventSequence, _aggregateRoot, false);
        _aggregateRoot._context = _aggregateRootContext;
        _aggregateRoot._mutation = _mutation;
    }
}
