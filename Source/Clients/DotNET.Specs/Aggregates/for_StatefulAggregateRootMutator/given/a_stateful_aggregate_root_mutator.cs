// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_StatefulAggregateRootMutator.given;

public class a_stateful_aggregate_root_mutator : Specification
{
    protected StatefulAggregateRootMutator<object> _mutator;
    protected AggregateRootState<object> _state;
    protected IAggregateRootStateProvider<object> _stateProvider;
    protected object _initialState;

    void Establish()
    {
        _state = new AggregateRootState<object>();
        _initialState = "Initial state";
        _state.SetState(_initialState);
        _stateProvider = Substitute.For<IAggregateRootStateProvider<object>>();
        _mutator = new StatefulAggregateRootMutator<object>(_state, _stateProvider);
    }
}
