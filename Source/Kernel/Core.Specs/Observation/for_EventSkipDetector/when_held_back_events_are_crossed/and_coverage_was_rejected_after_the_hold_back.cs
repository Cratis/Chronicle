// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_coverage_was_rejected_after_the_hold_back : given.a_detector
{
    void Because()
    {
        FillReachTracking(100, EventSkipDetector.MaxTrackedPartitions);
        HoldBack(_q, 10);
        _detector.Delivered(_q, 10UL);
        _detector.Evaluate(11);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
