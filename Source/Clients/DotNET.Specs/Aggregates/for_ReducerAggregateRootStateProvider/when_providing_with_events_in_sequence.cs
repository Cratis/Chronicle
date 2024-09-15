// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider;

public class when_providing_with_events_in_sequence : given.an_aggregate_root_that_handles_two_event_types_and_two_appended_events
{
    StateForAggregateRoot _state;
    StateForAggregateRoot _result;

    void Establish()
    {
        _state = new(Guid.NewGuid().ToString());
        _reducer.OnNext(events, null).Returns(new ReduceResult(_state, 2, [], string.Empty));
    }

    async Task Because() => _result = await _provider.Provide();

    [Fact] void should_return_the_state() => _result.ShouldEqual(_state);
}
