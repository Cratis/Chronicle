// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_StatefulAggregateRootMutator;

public class when_mutating : given.a_stateful_aggregate_root_mutator
{
    object _event;
    object _stateReturned;

    void Establish()
    {
        _event = "The event";
        _stateReturned = "Some kinda state";
        _stateProvider.Update(Arg.Any<object>(), Arg.Any<IEnumerable<object>>()).Returns(_stateReturned);
    }

    async Task Because() => await _mutator.Mutate(_event);

    [Fact] void should_call_update_on_state_provider() => _stateProvider.Received().Update(_initialState, Arg.Is<IEnumerable<object>>(_ => _.ToList().SequenceEqual(new[] { _event })));
    [Fact] void should_set_state_on_state() => _state.State.ShouldEqual(_stateReturned);
}
