// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reducers;

namespace Aksio.Cratis.Aggregates.for_ReducerAggregateRootStateProvider;

public class when_providing_with_events_in_sequence : given.an_aggregate_root_that_handles_two_event_types_and_two_appended_events
{
    StateForAggregateRoot state;
    StateForAggregateRoot result;

    void Establish()
    {
        state = new(Guid.NewGuid().ToString());

        reducer
            .Setup(_ => _.OnNext(events, null))
            .ReturnsAsync(new InternalReduceResult(state, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty));
    }

    async Task Because() => result = await provider.Provide() as StateForAggregateRoot;

    [Fact] void should_return_the_state() => result.ShouldEqual(state);
}
