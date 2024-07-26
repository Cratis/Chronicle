// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class a_stateful_aggregate_root : all_dependencies
{
    protected AggregateRoot<object> aggregate_root;
    protected EventSourceId event_source_id;
    protected IAggregateRootContext aggregate_root_context;

    void Establish()
    {
        aggregate_root = new();

        event_source_id = Guid.NewGuid().ToString();

        aggregate_root_context = new AggregateRootContext(CorrelationId.New(), event_source_id, event_sequence.Object, aggregate_root, false);
        aggregate_root._context = aggregate_root_context;
        aggregate_root._mutation = mutation.Object;
    }
}
