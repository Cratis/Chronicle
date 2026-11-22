// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_every_event_precedes_the_cursor : given.a_partition_run
{
    void Establish() => _context = _context with { NextEventSequenceNumber = 99UL };

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_distinguish_applicability_from_cursor_filtering() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_retain_no_stale_events() => _result.EventsToHandle.ShouldBeEmpty();
    [Fact] void should_keep_the_observed_tail() => _result.ObservedTail.ShouldEqual((EventSequenceNumber)12UL);
}
