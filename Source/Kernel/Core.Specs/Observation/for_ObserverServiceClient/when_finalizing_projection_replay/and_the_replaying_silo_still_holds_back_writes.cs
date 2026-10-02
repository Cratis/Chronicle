// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_projection_replay;

public class and_the_replaying_silo_still_holds_back_writes : Specification
{
    readonly List<string> _calls = [];
    IObserverService _idleSilo;
    IObserverService _replayingSilo;
    ObserverDetails _observerDetails;

    void Establish()
    {
        _observerDetails = new(new("observer", "event-store", "namespace", "event-log"), ObserverType.Projection);
        _idleSilo = SiloRecordingAs("idle");
        _replayingSilo = SiloRecordingAs("replaying");
    }

    Task Because() => ObserverServiceClient.FinalizeProjectionReplay([_idleSilo, _replayingSilo], _observerDetails);

    [Fact] void should_ask_every_silo_to_flush_before_any_silo_finalizes() =>
        _calls.Take(2).ShouldContainOnly("idle:flush", "replaying:flush");

    [Fact] void should_ask_every_silo_to_finalize_after_flushing() =>
        _calls.Skip(2).ShouldContainOnly("idle:finalize", "replaying:finalize");

    IObserverService SiloRecordingAs(string name)
    {
        var silo = Substitute.For<IObserverService>();
        silo.FlushReplayFor(_observerDetails).Returns(_ =>
        {
            lock (_calls) _calls.Add($"{name}:flush");
            return true;
        });
        silo.TryFinalizeReplayFor(_observerDetails).Returns(_ =>
        {
            lock (_calls) _calls.Add($"{name}:finalize");
            return true;
        });
        return silo;
    }
}
