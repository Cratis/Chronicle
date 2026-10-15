// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_a_round_reached_them : given.a_detector
{
    void Because()
    {
        _detector.Advanced(_p, 102UL, 104UL);
        _detector.RoundConcluded(105UL);
        Discard(_q, 106UL, 102);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
