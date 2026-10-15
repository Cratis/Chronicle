// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_the_event_is_at_or_above_next : given.a_detector
{
    void Because()
    {
        _detector.Advanced(_p, 100UL, 104UL);
        Discard(_q, 104UL, 104);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
