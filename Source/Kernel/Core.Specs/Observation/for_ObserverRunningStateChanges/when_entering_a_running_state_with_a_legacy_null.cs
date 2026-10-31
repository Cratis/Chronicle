// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_a_running_state_with_a_legacy_null : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverState _same;
    ObserverState _different;

    void Because()
    {
        var legacy = new ObserverState { RunningState = ObserverRunningState.Disconnected };
        (_same, _) = ObserverRunningStateChanges.Apply(legacy, ObserverRunningState.Disconnected, ObserverRunningState.Disconnected, _now);
        (_different, _) = ObserverRunningStateChanges.Apply(legacy, ObserverRunningState.Disconnected, ObserverRunningState.Active, _now);
    }

    [Fact] void should_not_backfill_the_same_state() => _same.RunningStateChangedAt.ShouldBeNull();
    [Fact] void should_stamp_a_real_transition() => _different.RunningStateChangedAt.ShouldEqual(_now);
}
