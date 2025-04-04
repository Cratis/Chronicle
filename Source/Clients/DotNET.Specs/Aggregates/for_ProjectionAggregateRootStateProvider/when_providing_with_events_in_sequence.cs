// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Aggregates.for_ProjectionAggregateRootStateProvider;

public class when_providing_with_events_in_sequence : given.an_aggregate_root_that_handles_two_event_types_and_two_appended_events
{
    StateForAggregateRoot _state;
    StateForAggregateRoot _result;

    ProjectionResult _projectionResult;

    void Establish()
    {
        _state = new(Guid.NewGuid().ToString());

        _projectionResult = new(_state, [], 2);
        _projections
            .GetInstanceByIdForSession(_correlationId, typeof(StateForAggregateRoot), _eventSourceId)
            .Returns(_projectionResult);
    }

    async Task Because() => _result = await _provider.Provide();

    [Fact] void should_return_the_state() => _result.ShouldEqual(_state);
}
