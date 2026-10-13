// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_completion_reached_them : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10);
        _detector.Completing(_q, 10UL);
        _detector.Release(_q, _detector.OwnershipTicket(_q));
        _detector.Evaluate(11);
    }

    [Fact] void should_not_report() => _reports.ShouldBeEmpty();
}
