// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_the_last_stream_is_closed : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _first;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _next;

    async Task Because()
    {
        _first = OpenReactor("first");
        await WaitForReactors(1);
        _first.Dispose();
        await _reactors.Calls[0].Detached.WaitAsync(_timeout);
        _next = OpenReactor("next");
        await WaitUntil(() => _reactors.Calls.Count == 2);
        await _reactors.Calls[1].Attached;
        _reactors.Calls[1].Emit(Events("served-by-the-new-connection"));
        await WaitUntil(() => _next.Received.Count == 1);
    }

    void Destroy() => _next.Dispose();

    [Fact] void should_open_a_new_call_for_the_next_stream() => _service.Calls.ShouldEqual(2);
    [Fact] void should_serve_the_next_stream() => _next.Received.Single().Partition.ShouldEqual("served-by-the-new-connection");
}
