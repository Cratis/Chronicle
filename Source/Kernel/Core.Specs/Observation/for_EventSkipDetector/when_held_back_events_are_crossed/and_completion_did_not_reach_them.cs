// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_held_back_events_are_crossed;

public class and_completion_did_not_reach_them : given.a_detector
{
    void Because()
    {
        HoldBack(_q, 10, 20);
        _detector.Completing(_q, 10UL);
        _detector.Release(_q, _detector.OwnershipTicket(_q));
        _detector.Evaluate(21);
    }

    [Fact] void should_report_the_event_beyond_its_reach() => _reports.Count.ShouldEqual(1);
    [Fact] void should_report_the_uncovered_sequence_number() => _reports[0].First.ShouldEqual((EventSequenceNumber)20UL);
}
