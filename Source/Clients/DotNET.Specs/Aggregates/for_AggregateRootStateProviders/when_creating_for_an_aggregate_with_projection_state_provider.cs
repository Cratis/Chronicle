// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRootStateProviders;

public class when_creating_for_an_aggregate_with_projection_state_provider : given.aggregate_root_state_providers
{
    IAggregateRootStateProvider<StateForAggregateRoot> _result;

    void Establish()
    {
        _reducers.HasReducerFor(typeof(StateForAggregateRoot)).Returns(false);
        _projections.HasFor(typeof(StateForAggregateRoot)).Returns(true);
    }

    async Task Because() => _result = await _stateProviders.CreateFor<StateForAggregateRoot>(_aggregateRootContext);

    [Fact] void should_return_reducer_state_provider() => _result.ShouldBeOfExactType<ProjectionAggregateRootStateProvider<StateForAggregateRoot>>();
}
