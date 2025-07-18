// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRootStateProviders;

public class when_creating_for_an_aggregate_without_state_provider : given.aggregate_root_state_providers
{
    Exception _result;

    void Establish()
    {
        _reducers.HasReducerFor(typeof(StateForAggregateRoot)).Returns(false);
        _projections.HasFor(typeof(StateForAggregateRoot)).Returns(false);
    }

    async Task Because() => _result = await Catch.Exception(() => _stateProviders.CreateFor<StateForAggregateRoot>(_aggregateRootContext));

    [Fact] void should_throw_missing_state_provider_exception() => _result.ShouldBeOfExactType<MissingAggregateRootStateProvider>();
}
