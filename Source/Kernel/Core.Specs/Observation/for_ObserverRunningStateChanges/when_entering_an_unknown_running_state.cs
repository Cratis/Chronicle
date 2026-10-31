// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_an_unknown_running_state : Specification
{
    readonly DateTimeOffset _previous = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    (ObserverState State, ObserverRunningState Settled) _result;

    void Because() => _result = ObserverRunningStateChanges.Apply(new() { RunningState = ObserverRunningState.Disconnected, RunningStateChangedAt = _previous }, ObserverRunningState.Disconnected, ObserverRunningState.Unknown, _previous.AddHours(1));

    [Fact] void should_keep_the_timestamp() => _result.State.RunningStateChangedAt.ShouldEqual(_previous);
    [Fact] void should_record_the_transient_running_state() => _result.State.RunningState.ShouldEqual(ObserverRunningState.Unknown);
    [Fact] void should_keep_the_last_settled_state() => _result.Settled.ShouldEqual(ObserverRunningState.Disconnected);
}
