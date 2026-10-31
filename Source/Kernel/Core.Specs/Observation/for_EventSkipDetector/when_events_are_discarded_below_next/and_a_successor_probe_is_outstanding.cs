// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_a_successor_probe_is_outstanding : given.a_detector
{
    int _reportsWhileOutstanding;

    void Because()
    {
        // The recovery completed at 100 and the probe for its successor has not settled yet.
        _detector.Completing(_q, 100UL);
        var ticket = _detector.OwnershipTicket(_q);
        _detector.Advanced(_p, 102UL, 104UL);
        Discard(_q, 104UL, 102);
        _reportsWhileOutstanding = _reports.Count;
        _detector.Release(_q, ticket);
        Discard(_q, 104UL, 102);
    }

    [Fact] void should_not_report_while_the_probe_is_outstanding() => _reportsWhileOutstanding.ShouldEqual(0);
    [Fact] void should_report_once_the_probe_is_settled() => _reports.Count.ShouldEqual(1);
}
