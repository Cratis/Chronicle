// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_the_same_running_state : Specification
{
    readonly DateTimeOffset _previous = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    (ObserverState State, ObserverRunningState Settled) _result;

    void Because() => _result = ObserverRunningStateChanges.Apply(new() { RunningState = ObserverRunningState.Unknown, RunningStateChangedAt = _previous }, ObserverRunningState.Disconnected, ObserverRunningState.Disconnected, _previous.AddHours(1));

    [Fact] void should_keep_the_original_timestamp() => _result.State.RunningStateChangedAt.ShouldEqual(_previous);
    [Fact] void should_restore_the_running_state_after_routing() => _result.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_keep_the_settled_state() => _result.Settled.ShouldEqual(ObserverRunningState.Disconnected);
}
