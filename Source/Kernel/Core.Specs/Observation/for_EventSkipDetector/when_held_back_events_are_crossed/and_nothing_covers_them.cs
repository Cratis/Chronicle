// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_nothing_covers_them : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10);
        _detector.Evaluate(11);
    }

    [Fact] void should_report_one_suspected_skip() => _reports.Count.ShouldEqual(1);
    [Fact] void should_report_the_partition() => _reports[0].Partition.ShouldEqual(_q);
    [Fact] void should_report_the_sequence_number() => _reports[0].First.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_report_the_reason() => _reports[0].Reason.ShouldEqual(SuspectedSkipReason.HeldBackWhileCatchingUp);
}
