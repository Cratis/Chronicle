// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_no_event_type_is_subscribed : given.a_partition_run
{
    void Establish() => _context = _context with { EventTypeIds = new HashSet<EventTypeId>() };

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_skip_the_run() => _result.IsApplicable.ShouldBeFalse();
    [Fact] void should_still_observe_the_tail() => _result.ObservedTail.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_retain_no_events_for_delivery() => _result.EventsToHandle.ShouldBeEmpty();
}
