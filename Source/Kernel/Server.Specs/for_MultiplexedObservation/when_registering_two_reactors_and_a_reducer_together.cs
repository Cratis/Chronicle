// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_registering_two_reactors_and_a_reducer_together : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _first;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _second;
    given.OpenStream<ReducerMessage, ReduceOperationMessage> _reducer;

    async Task Because()
    {
        _first = OpenReactor("first");
        _second = OpenReactor("second");
        _reducer = OpenReducer("reducer");
        await WaitUntil(() => _reactors.Calls.Count == 2 && _reducers.Calls.Count == 1 && _reactors.Calls.All(_ => _.Received.Count == 1) && _reducers.Calls[0].Received.Count == 1);
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _reducer.Dispose();
    }

    [Fact] void should_serve_each_reactor_by_its_own_observe_call() => _reactors.Calls.Count.ShouldEqual(2);
    [Fact] void should_serve_the_reducer_by_its_own_observe_call() => _reducers.Calls.Count.ShouldEqual(1);
    [Fact] void should_hand_each_reactor_its_registration() => _reactors.Calls.Select(_ => ReactorIdOf(_.Received[0])).Order().ShouldEqual(["first", "second"]);
    [Fact] void should_hand_the_reducer_its_registration() => ((RegisterReducer)_reducers.Calls[0].Received[0].Content.Value!).Reducer.ReducerId.ShouldEqual("reducer");
    [Fact] void should_open_one_call_to_the_kernel() => _service.Calls.ShouldEqual(1);
    [Fact] void should_register_all_three_in_one_write() => _service.Writes.Single().ShouldEqual(3);
}
