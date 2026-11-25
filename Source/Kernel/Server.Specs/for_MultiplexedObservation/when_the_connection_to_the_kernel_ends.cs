// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_the_connection_to_the_kernel_ends : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _reactor;
    given.OpenStream<ReducerMessage, ReduceOperationMessage> _reducer;

    async Task Because()
    {
        _reactor = OpenReactor("reactor");
        _reducer = OpenReducer("reducer");
        await WaitForReactors(1);
        await WaitForReducers(1);
        _grpc.Dispose();
        await Task.WhenAll(_reactors.Calls[0].Detached, _reducers.Calls[0].Detached).WaitAsync(_timeout);
        await WaitUntil(() => _reactor.Error is not null && _reducer.Error is not null);
    }

    void Destroy()
    {
        _reactor.Dispose();
        _reducer.Dispose();
    }

    [Fact] void should_end_every_stream_on_the_kernel() => (_reactors.Calls[0].Detached.IsCompleted && _reducers.Calls[0].Detached.IsCompleted).ShouldBeTrue();
    [Fact] void should_fail_the_reactor_stream() => _reactor.Error.ShouldNotBeNull();
    [Fact] void should_fail_the_reducer_stream() => _reducer.Error.ShouldNotBeNull();
}
