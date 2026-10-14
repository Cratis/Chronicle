// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_a_job_reported_the_partition_before_the_hold_back : given.a_detector
{
    void Because()
    {
        _detector.JobReported(_q);
        HoldBack(_q, 10);
        _detector.Evaluate(11);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
