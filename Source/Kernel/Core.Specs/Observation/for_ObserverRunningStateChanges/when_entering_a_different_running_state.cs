// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_a_different_running_state : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    readonly ObserverRunningState[] _next = [ObserverRunningState.Disconnected, ObserverRunningState.Active, ObserverRunningState.Replaying, ObserverRunningState.Quarantined];
    (DateTimeOffset? ChangedAt, ObserverRunningState Settled, bool Changed)[] _results;

    void Because() => _results = _next.Select(next => ObserverRunningStateChanges.Apply(null, ObserverRunningState.Unknown, next, _now)).ToArray();

    [Fact] void should_stamp_the_transition_time() => _results.ShouldEachConformTo(result => result.ChangedAt == _now);
    [Fact] void should_report_the_change() => _results.ShouldEachConformTo(result => result.Changed);
    [Fact] void should_remember_the_settled_state() => _results.Select(result => result.Settled).ShouldEqual(_next);
}
