// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_more_than_16_were_held_for_one_partition : given.a_detector
{
    void Because()
    {
        HoldBack(_q, Enumerable.Range(1, 20).Select(_ => (ulong)_).ToArray());
        _detector.Evaluate(100);
    }

    [Fact] void should_report_at_most_the_tracked_events() => _reports.Sum(_ => _.Count).ShouldEqual(EventSkipDetector.MaxSequenceNumbersPerPartition);
}
