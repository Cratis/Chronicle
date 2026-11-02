// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_replay_notifications;

public class and_global_callbacks_bracket_event_handling : given.a_notifiable_reactor_stream
{
    bool _beginWasIncomplete;
    bool _endWasIncomplete;
    ReactorResult[] _results;

    async Task Because()
    {
        Deliver("other-order", "prior", 101);
        await _work.Entered("prior");
        Notify(ReplayState.BeginReplay);
        Deliver("replayed-order", "replay", 102);
        Notify(ReplayState.EndReplay);
        Deliver("other-order", "live", 103);

        _work.Release("prior");
        await _work.Entered("begin");
        _beginWasIncomplete = !_work.IsCompleted("begin");
        _work.Release("begin");
        await _work.Entered("replay");
        _work.Release("replay");
        await _work.Entered("end");
        _endWasIncomplete = !_work.IsCompleted("end");
        _work.Release("end");
        await _work.Entered("live");
        _work.Release("live");
        _results = [await Answered(101), await Answered(102), await Answered(103)];
    }

    [Fact] void should_await_an_asynchronous_begin_callback() => _beginWasIncomplete.ShouldBeTrue();
    [Fact] void should_await_an_asynchronous_end_callback() => _endWasIncomplete.ShouldBeTrue();
    [Fact] void should_bracket_replay_handling_across_partitions() => _work.Trace.ShouldEqual<string>([
        "prior:entered", "prior:completed",
        "begin:entered", "begin:completed",
        "replay:entered", "replay:completed",
        "end:entered", "end:completed",
        "live:entered", "live:completed"
    ]);
    [Fact] void should_successfully_handle_every_event() => _results.Select(result => result.State).ShouldEqual([ObservationState.Success, ObservationState.Success, ObservationState.Success]);
}
