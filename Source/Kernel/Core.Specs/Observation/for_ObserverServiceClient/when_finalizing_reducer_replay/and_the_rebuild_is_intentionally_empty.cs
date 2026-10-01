// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_finalizing_reducer_replay;

public class and_the_rebuild_is_intentionally_empty : Specification
{
    readonly List<string> _calls = [];
    IObserverService _first;
    IObserverService _second;
    ObserverDetails _details;

    void Establish()
    {
        _details = new(new("reducer", "store", "namespace", "event-log"), ObserverType.Reducer) { ReplaySucceededWithEvents = true };
        _first = Substitute.For<IObserverService>();
        _second = Substitute.For<IObserverService>();
        _first.FlushReplayFor(_details).Returns(_ =>
        {
            _calls.Add("first:flush");
            return true;
        });
        _second.FlushReplayFor(_details).Returns(_ =>
        {
            _calls.Add("second:flush");
            return true;
        });
        _first.TryFinalizeReplayFor(_details).Returns(_ =>
        {
            _calls.Add("first:promote");
            return true;
        });
        _second.TryFinalizeReplayFor(Arg.Any<ObserverDetails>()).Returns(_ =>
        {
            _calls.Add("second:leave");
            return true;
        });
    }

    Task Because() => ObserverServiceClient.FinalizeProjectionReplay([_first, _second], _details);

    [Fact] void should_flush_every_silo_before_promoting() => _calls.Take(2).ShouldContainOnly("first:flush", "second:flush");
    [Fact] void should_promote_once_and_leave_replay_on_the_other_silo() => _calls.Skip(2).ShouldContainOnly("first:promote", "second:leave");
    [Fact] void should_not_allow_a_second_empty_promotion() => _second.Received(1).TryFinalizeReplayFor(Arg.Is<ObserverDetails>(details => details.ReplayAlreadyFinalized));
}
