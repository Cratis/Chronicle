// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_a_job_started_before_the_probe_settled : given.a_detector
{
    void Because()
    {
        _detector.Completing(_q, 100UL);
        var ticket = _detector.OwnershipTicket(_q);
        _detector.JobStarted(_q);
        _detector.Release(_q, ticket);
        _detector.Advanced(_p, 102UL, 104UL);
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_keep_owning_the_partition() => _reports.ShouldBeEmpty();
}
