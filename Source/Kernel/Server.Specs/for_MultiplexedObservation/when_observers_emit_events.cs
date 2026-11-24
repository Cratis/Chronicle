// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_observers_emit_events : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _first;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _second;
    given.OpenStream<ReducerMessage, ReduceOperationMessage> _reducer;

    async Task Because()
    {
        _first = OpenReactor("first");
        _second = OpenReactor("second");
        _reducer = OpenReducer("reducer");
        await WaitForReactors(2);
        await WaitForReducers(1);

        for (var index = 1; index <= 20; index++)
        {
            _reactors.Calls[0].Emit(Events($"first-{index}"));
            _reactors.Calls[1].Emit(Events($"second-{index}"));
            _reducers.Calls[0].Emit(new ReduceOperationMessage { Partition = $"reducer-{index}" });
        }

        await WaitUntil(() => _first.Received.Count == 20 && _second.Received.Count == 20 && _reducer.Received.Count == 20);
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _reducer.Dispose();
    }

    [Fact] void should_deliver_to_the_first_reactor_only_its_events_in_order() => _first.Received.Select(_ => _.Partition).ShouldEqual(Enumerable.Range(1, 20).Select(_ => $"first-{_}"));
    [Fact] void should_deliver_to_the_second_reactor_only_its_events_in_order() => _second.Received.Select(_ => _.Partition).ShouldEqual(Enumerable.Range(1, 20).Select(_ => $"second-{_}"));
    [Fact] void should_deliver_to_the_reducer_only_its_operations_in_order() => _reducer.Received.Select(_ => _.Partition).ShouldEqual(Enumerable.Range(1, 20).Select(_ => $"reducer-{_}"));
}
