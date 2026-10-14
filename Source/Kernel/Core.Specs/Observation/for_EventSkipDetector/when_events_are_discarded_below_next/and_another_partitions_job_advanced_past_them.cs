// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_another_partitions_job_advanced_past_them : given.a_detector
{
    void Because()
    {
        _detector.Advanced(_p, 102UL, 104UL);
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_report_one_suspected_skip() => _reports.Count.ShouldEqual(1);
    [Fact] void should_report_the_partition() => _reports[0].Partition.ShouldEqual(_q);
    [Fact] void should_report_the_sequence_number() => _reports[0].First.ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_report_the_reason() => _reports[0].Reason.ShouldEqual(SuspectedSkipReason.PassedByPartitionJob);
}
