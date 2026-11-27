// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_position_is_stuck_while_the_tail_advances : given.a_tracker
{
    ObserverLagSample _first;
    ObserverLagSample _result;

    void Establish() => _first = Measure(0, 10);

    void Because()
    {
        Measure(2, 11);
        _result = Measure(4, 12);
    }

    [Fact] void should_have_started_lagging() => _first.Status.ShouldEqual(ObserverLagStatus.Lagging);
    [Fact] void should_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Stalled);
    [Fact] void should_report_the_distance_in_slots() => _result.Distance.ShouldEqual(8UL);
}
