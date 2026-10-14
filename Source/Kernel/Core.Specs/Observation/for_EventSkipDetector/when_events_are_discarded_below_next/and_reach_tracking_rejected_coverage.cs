// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_reach_tracking_rejected_coverage : given.a_detector
{
    void Because()
    {
        FillReachTracking(200, EventSkipDetector.MaxTrackedPartitions);
        _detector.Delivered(_q, 102UL);
        _detector.Advanced(_p, 102UL, 104UL);
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
