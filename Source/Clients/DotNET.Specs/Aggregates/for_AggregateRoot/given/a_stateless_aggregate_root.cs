// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class a_stateless_aggregate_root : all_dependencies
{
    protected StatelessAggregateRoot _aggregateRoot;
    protected EventSourceId _eventSourceId;
    protected IAggregateRootContext _aggregateRootContext;

    void Establish()
    {
        _aggregateRoot = new();
        _eventSourceId = Guid.NewGuid().ToString();
        _aggregateRootContext = new AggregateRootContext(CorrelationId.New(), _eventSourceId, event_sequence.Object, _aggregateRoot, false);

        _aggregateRoot._context = _aggregateRootContext;
        _aggregateRoot._mutation = mutation.Object;
    }
}
