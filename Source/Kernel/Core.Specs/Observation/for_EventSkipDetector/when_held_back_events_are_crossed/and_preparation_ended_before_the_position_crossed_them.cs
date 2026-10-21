// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_preparation_ended_before_the_position_crossed_them : given.a_detector
{
    void Because()
    {
        _detector.HeldBack(_q, [102UL], SuspectedSkipReason.HeldBackWhilePreparingCatchUp);
        _detector.Evaluate(102UL);
        _detector.PreparationEnded();
        _detector.Evaluate(104UL);
    }

    [Fact] void should_report_the_event_once_the_position_passes_it() => _reports.Count.ShouldEqual(1);
}
