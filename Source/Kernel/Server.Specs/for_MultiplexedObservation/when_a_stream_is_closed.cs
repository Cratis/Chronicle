// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_a_stream_is_closed : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _closing;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _staying;
    int _closingIndex;

    async Task Because()
    {
        _closing = OpenReactor("closing");
        _staying = OpenReactor("staying");
        await WaitForReactors(2);
        _closingIndex = ReactorIdOf(_reactors.Calls[0].Received[0]) == "closing" ? 0 : 1;
        _closing.Dispose();
        await _reactors.Calls[_closingIndex].Detached.WaitAsync(_timeout);
    }

    void Destroy() => _staying.Dispose();

    [Fact] void should_end_the_kernel_side_of_that_stream() => _reactors.Calls[_closingIndex].Detached.IsCompleted.ShouldBeTrue();
    [Fact] void should_leave_the_other_stream_running() => _reactors.Calls[1 - _closingIndex].Detached.IsCompleted.ShouldBeFalse();
    [Fact] void should_keep_the_connection_to_the_kernel() => _service.Calls.ShouldEqual(1);
}
