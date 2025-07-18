// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot;

public class when_applying_to_stateless_aggregate_root : given.a_stateless_aggregate_root
{
    FirstEventType _eventToApply;
    StateForAggregateRoot _state;

    void Establish()
    {
        _eventToApply = new(Guid.NewGuid().ToString());
        _state = new StateForAggregateRoot(Guid.NewGuid().ToString());
    }

    async Task Because() => await _aggregateRoot.Apply(_eventToApply);

    [Fact] void should_forward_to_mutation() => _mutation.Received(1).Apply(_eventToApply);
}
