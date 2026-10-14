// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_events_are_discarded_below_next;

public class and_nothing_advanced_the_position : given.a_detector
{
    void Because() => Discard(_q, 104UL, 102);

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
