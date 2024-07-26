// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_StatefulAggregateRootMutator;

public class when_rehydrating : given.a_stateful_aggregate_root_mutator
{
    object _stateReturned;

    void Establish()
    {
        _stateReturned = "Some kinda state";
        _stateProvider.Provide().Returns(_stateReturned);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_call_provide_on_state_provider() => _stateProvider.Received().Provide();
    [Fact] void should_set_state_on_state() => _state.State.ShouldEqual(_stateReturned);
}
