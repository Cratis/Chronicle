// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_resetting;

public class and_a_partition_was_owned_and_a_position_advanced : given.a_detector
{
    void Because()
    {
        _detector.JobStarted(_q);
        _detector.Advanced(_p, 102UL, 104UL);
        _detector.Reset();
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_not_report_because_the_advance_is_forgotten() => _reports.ShouldBeEmpty();
    [Fact] void should_have_forgotten_the_ownership() => _detector.OwnershipTicket(_q).ShouldEqual(0L);
}
