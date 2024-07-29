// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class a_reducer_aggregate_root_state_provider : Specification
{
    protected ReducerAggregateRootStateProvider<StateForAggregateRoot> _provider;
    protected IReducerHandler _reducer;
    protected IReducerInvoker _invoker;
    protected IEventSequence _eventSequence;
    protected IAggregateRootEventHandlers _eventHandlers;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();

        _reducer = Substitute.For<IReducerHandler>();
        _invoker = Substitute.For<IReducerInvoker>();
        _reducer.Invoker.Returns(_invoker);
    }
}
