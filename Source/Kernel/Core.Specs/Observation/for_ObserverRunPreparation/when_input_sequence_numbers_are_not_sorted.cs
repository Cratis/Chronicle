// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_input_sequence_numbers_are_not_sorted : given.a_partition_run
{
    void Establish()
    {
        _events = [Event(_subscribedType, 42), Event(_otherType, 17)];
        _context = _context with { NextEventSequenceNumber = 0UL };
    }

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_use_the_last_input_not_the_maximum_as_the_tail() => _result.ObservedTail.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_not_reorder_the_events() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(42UL, 17UL);
}
