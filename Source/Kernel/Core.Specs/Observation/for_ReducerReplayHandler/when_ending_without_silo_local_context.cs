// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler;

public class when_ending_without_silo_local_context : Specification
{
    IReducerMediator _mediator;
    ReducerReplayHandler _handler;
    ObserverDetails _details;
    Result<ICanHandleReplayForObserver.Error> _result;
    void Establish()
    {
        _mediator = Substitute.For<IReducerMediator>();
        _handler = new(_mediator);
        _details = new(new("reducer", "store", "namespace", "event-log"), ObserverType.Reducer);
    }
    async Task Because() => _result = await _handler.EndReplayFor(_details);
    [Fact] void should_notify_clients_without_attempting_a_local_swap() => _mediator.Received(1).OnEndReplay(new ReducerId("reducer"), _details.Key.EventStore, _details.Key.Namespace);
    [Fact] void should_not_depend_on_a_silo_local_replay_context() => _result.IsSuccess.ShouldBeTrue();
}
