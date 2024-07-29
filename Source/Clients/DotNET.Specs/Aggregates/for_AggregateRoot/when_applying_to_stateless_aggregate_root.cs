// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot;

public class when_applying_to_stateless_aggregate_root : given.a_stateless_aggregate_root
{
    FirstEventType event_to_apply;
    StateForAggregateRoot state;

    void Establish()
    {
        event_to_apply = new(Guid.NewGuid().ToString());
        state = new StateForAggregateRoot(Guid.NewGuid().ToString());
    }

    void Because() => _aggregateRoot.Apply(event_to_apply);

    [Fact] void should_forward_to_mutation() => _mutation.Received(1).Apply(event_to_apply);
}
