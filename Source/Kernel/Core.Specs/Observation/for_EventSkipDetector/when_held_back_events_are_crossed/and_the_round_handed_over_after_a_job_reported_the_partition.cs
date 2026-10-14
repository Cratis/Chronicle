// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_the_round_handed_over_after_a_job_reported_the_partition : given.a_detector
{
    void Because()
    {
        _detector.JobReported(_q);
        _detector.ClearJobReported();
        HoldBack(_q, 10);
        _detector.Evaluate(11);
    }

    [Fact] void should_report_events_held_back_in_the_next_round() => _reports.Count.ShouldEqual(1);
}
