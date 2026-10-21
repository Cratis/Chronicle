// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_the_position_is_at_the_held_back_event : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10);
        _detector.Evaluate(10);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
