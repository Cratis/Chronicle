// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_subscribing_to_all_event_types : given.a_partition_run
{
    void Establish() => _context = _context with { SubscribesToAllEvents = true, EventTypeIds = new HashSet<EventTypeId>() };

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_not_require_a_fixed_event_type_list() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_still_apply_the_cursor() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(11UL, 12UL);
}
