// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_more_than_256_partitions_were_held : given.a_detector
{
    void Because()
    {
        for (var i = 0; i < EventSkipDetector.MaxTrackedPartitions + 1; i++)
        {
            HoldBack($"partition-{i}", 10);
        }

        _detector.Evaluate(11);
    }

    [Fact] void should_only_report_the_tracked_partitions() => _reports.Count.ShouldEqual(EventSkipDetector.MaxTrackedPartitions);
}
