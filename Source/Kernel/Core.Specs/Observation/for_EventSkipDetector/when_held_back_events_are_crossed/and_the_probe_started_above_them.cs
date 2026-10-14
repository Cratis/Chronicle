// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_the_probe_started_above_them : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10);
        _detector.ProbedFrom(_q, 11UL);
        _detector.Evaluate(11);
    }

    [Fact] void should_report() => _reports.Count.ShouldEqual(1);
}
