// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_one_stream_completes : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _finishing;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _other;

    async Task Because()
    {
        _finishing = OpenReactor("finishing");
        _other = OpenReactor("other");
        await WaitForReactors(2);
        var finishingIndex = ReactorIdOf(_reactors.Calls[0].Received[0]) == "finishing" ? 0 : 1;
        _reactors.Calls[finishingIndex].Emit(Events("last"));
        _reactors.Calls[finishingIndex].Complete();
        await WaitUntil(() => _finishing.Completed);
    }

    void Destroy()
    {
        _finishing.Dispose();
        _other.Dispose();
    }

    [Fact] void should_deliver_what_came_before_the_end() => _finishing.Received.Single().Partition.ShouldEqual("last");
    [Fact] void should_complete_the_stream() => _finishing.Completed.ShouldBeTrue();
    [Fact] void should_not_complete_the_other_stream() => _other.Completed.ShouldBeFalse();
}
