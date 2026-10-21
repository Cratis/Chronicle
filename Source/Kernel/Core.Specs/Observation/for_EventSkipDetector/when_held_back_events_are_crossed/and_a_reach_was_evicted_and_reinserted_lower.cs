// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_a_reach_was_evicted_and_reinserted_lower : given.a_detector
{
    void Because()
    {
        _detector.Delivered(_q, 102UL);
        FillReachTracking(200, EventSkipDetector.MaxTrackedPartitions - 1);

        // A newer, further reach evicts the lowest one, which is the partition under suspicion.
        _detector.Delivered(_p, 400UL);
        _detector.Delivered(_q, 9UL);
        HoldBack(_q, 50);
        _detector.Evaluate(300);
    }

    [Fact] void should_not_report_events_the_evicted_reach_covered() => _reports.ShouldBeEmpty();
}
