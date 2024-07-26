// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_AggregateRootStateProviders;

public class when_creating_for_an_aggregate_with_more_than_one_state_provider : given.aggregate_root_state_providers
{
    Exception result;

    void Establish()
    {
        _reducers.HasReducerFor(typeof(StateForAggregateRoot)).Returns(true);
        _projections.HasProjectionFor(typeof(StateForAggregateRoot)).Returns(true);
    }

    async Task Because() => result = await Catch.Exception(() => _stateProviders.CreateFor<StateForAggregateRoot>(_aggregateRootContext));

    [Fact] void should_throw_ambiguous_state_provider_exception() => result.ShouldBeOfExactType<AmbiguousAggregateRootStateProvider>();
}
