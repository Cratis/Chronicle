// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_a_later_event_is_held_back_while_catching_up_after_preparation_was_held : given.a_detector
{
    void Because()
    {
        _detector.HeldBack(_q, [100UL], SuspectedSkipReason.HeldBackWhilePreparingCatchUp);
        _detector.HeldBack(_q, [102UL], SuspectedSkipReason.HeldBackWhileCatchingUp);
        _detector.Evaluate(104UL);
    }

    [Fact] void should_report_only_the_event_not_owned_by_preparation() => _reports.Count.ShouldEqual(1);
    [Fact] void should_report_it_with_its_own_reason() => _reports[0].Reason.ShouldEqual(SuspectedSkipReason.HeldBackWhileCatchingUp);
    [Fact] void should_report_its_sequence_number() => _reports[0].First.Value.ShouldEqual(102UL);
}
