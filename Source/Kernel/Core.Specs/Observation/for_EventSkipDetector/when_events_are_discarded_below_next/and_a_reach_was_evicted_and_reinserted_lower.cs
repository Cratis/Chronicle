// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_a_reach_was_evicted_and_reinserted_lower : given.a_detector
{
    void Because()
    {
        _detector.Delivered(_q, 102UL);
        FillReachTracking(200, EventSkipDetector.MaxTrackedPartitions - 1);
        _detector.Delivered(_p, 400UL);
        _detector.Delivered(_q, 9UL);
        _detector.Advanced("R", 90UL, 110UL);
        Discard(_q, 110UL, 100);
    }

    [Fact] void should_not_report_events_the_evicted_reach_covered() => _reports.ShouldBeEmpty();
}
