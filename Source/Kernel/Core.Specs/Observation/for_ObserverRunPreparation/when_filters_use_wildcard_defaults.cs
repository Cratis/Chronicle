// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_filters_use_wildcard_defaults : given.a_partition_run
{
    void Establish() => _context = _context with { Filters = new([], EventSourceType.Unspecified, EventStreamType.All) };

    void Because() => _result = ObserverRunPreparation.Prepare(_events, _context);

    [Fact] void should_apply_no_filter_for_empty_tags_or_wildcard_types() => _result.IsApplicable.ShouldBeTrue();
    [Fact] void should_retain_the_cursor_suffix() => _result.EventsToHandle.Select(appendedEvent => appendedEvent.Context.SequenceNumber).ShouldEqual<EventSequenceNumber>(11UL, 12UL);
}
