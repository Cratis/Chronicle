// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_they_were_held_back_while_preparing_a_catch_up : given.a_detector
{
    int _reportsBeforeHandover;

    void Because()
    {
        _detector.HeldBack(_q, [10UL], SuspectedSkipReason.HeldBackWhilePreparingCatchUp);
        _detector.Evaluate(11UL);
        _reportsBeforeHandover = _reports.Count;
        _detector.Evaluate(11UL, atRoundHandover: true);
    }

    [Fact] void should_not_report_before_the_round_hands_over() => _reportsBeforeHandover.ShouldEqual(0);
    [Fact] void should_report_at_the_handover() => _reports.Count.ShouldEqual(1);
}
