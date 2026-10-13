// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_the_partition_is_released_afterwards : given.a_detector
{
    void Because()
    {
        _detector.JobStarted(_q);
        HoldBack(_q, 10);
        _detector.Evaluate(11);
        _detector.Release(_q, _detector.OwnershipTicket(_q));
        _detector.Evaluate(11);
    }

    [Fact] void should_report_once_the_owner_is_gone() => _reports.Count.ShouldEqual(1);
}
