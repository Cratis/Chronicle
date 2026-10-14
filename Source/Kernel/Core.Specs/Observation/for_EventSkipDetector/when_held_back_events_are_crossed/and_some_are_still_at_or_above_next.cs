// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_some_are_still_at_or_above_next : given.a_detector
{
    int _reportsAfterFirstEvaluation;

    void Because()
    {
        HoldBack(_q, 10, 20);
        _detector.Evaluate(13);
        _reportsAfterFirstEvaluation = _reports.Count;
        _detector.Evaluate(21);
    }

    [Fact] void should_only_report_the_crossed_one_first() => _reportsAfterFirstEvaluation.ShouldEqual(1);
    [Fact] void should_report_the_other_when_it_is_crossed() => _reports.Count.ShouldEqual(2);
    [Fact] void should_report_the_later_sequence_number_last() => _reports[1].First.ShouldEqual((EventSequenceNumber)20UL);
    [Fact] void should_only_count_the_individual_event() => _reports.TrueForAll(_ => _.Count == 1).ShouldBeTrue();
}
