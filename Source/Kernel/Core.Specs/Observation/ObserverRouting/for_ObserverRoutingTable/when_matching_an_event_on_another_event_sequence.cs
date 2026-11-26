// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRoutingTable;

public class when_matching_an_event_on_another_event_sequence : given.a_routing_table
{
    ObserverId[] _matched;

    void Because() => _matched = [.. _table.Match(EventSequenceId.Outbox, _firstEventType.Id).Select(_ => _.ObserverId)];

    [Fact] void should_match_only_the_observers_of_that_event_sequence() => _matched.ShouldContainOnly(_outboxObserver);
}
