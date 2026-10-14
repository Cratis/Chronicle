// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_job_reports_are_saturated : given.a_detector
{
    void Because()
    {
        for (var i = 0; i < EventSkipDetector.MaxTrackedPartitions + 1; i++)
        {
            _detector.JobReported($"partition-{i}");
        }

        _detector.Advanced(_p, 102UL, 104UL);
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
