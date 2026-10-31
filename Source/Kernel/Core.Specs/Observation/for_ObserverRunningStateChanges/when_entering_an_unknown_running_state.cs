// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_an_unknown_running_state : Specification
{
    readonly DateTimeOffset _previous = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    (DateTimeOffset? ChangedAt, ObserverRunningState Settled, bool Changed) _result;

    void Because() => _result = ObserverRunningStateChanges.Apply(_previous, ObserverRunningState.Disconnected, ObserverRunningState.Unknown, _previous.AddHours(1));

    [Fact] void should_keep_the_timestamp() => _result.ChangedAt.ShouldEqual(_previous);
    [Fact] void should_not_report_a_change() => _result.Changed.ShouldBeFalse();
    [Fact] void should_keep_the_last_settled_state() => _result.Settled.ShouldEqual(ObserverRunningState.Disconnected);
}
