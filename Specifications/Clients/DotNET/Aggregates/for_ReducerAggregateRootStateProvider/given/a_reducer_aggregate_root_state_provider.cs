// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class a_reducer_aggregate_root_state_provider : Specification
{
    protected ReducerAggregateRootStateProvider provider;
    protected Mock<IReducerHandler> reducer;
    protected Mock<IReducerInvoker> invoker;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IAggregateRootEventHandlers> event_handlers;

    void Establish()
    {
        event_sequence = new();
        event_handlers = new();

        reducer = new();
        invoker = new();
        reducer.SetupGet(_ => _.Invoker).Returns(invoker.Object);
    }
}
