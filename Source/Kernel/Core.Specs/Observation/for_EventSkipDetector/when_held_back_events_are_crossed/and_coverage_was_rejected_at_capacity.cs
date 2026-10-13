// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_coverage_was_rejected_at_capacity : given.a_detector
{
    void Because()
    {
        FillReachTracking(100, EventSkipDetector.MaxTrackedPartitions);

        // Lower than everything tracked, so it is rejected and only the unknown coverage ceiling remembers it.
        _detector.Delivered(_q, 10UL);
        HoldBack(_q, 10);
        HoldBack(_p, 500);
        _detector.Evaluate(600);
    }

    [Fact] void should_not_report_the_event_at_the_ceiling() => _reports.Exists(_ => _.Partition == _q).ShouldBeFalse();
    [Fact] void should_still_report_events_above_the_ceiling() => _reports.Count(_ => _.Partition == _p).ShouldEqual(1);
}
