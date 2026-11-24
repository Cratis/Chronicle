// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_ObserverRoutingTable;

public class when_matching_an_event : given.a_routing_table
{
    ObserverId[] _matched;

    void Because() => _matched = [.. _table.Match(EventSequenceId.Log, _secondEventType.Id).Select(_ => _.ObserverId)];

    [Fact] void should_match_the_observers_of_the_event_type_and_of_every_event_type() => _matched.ShouldContainOnly(_secondObserver, _everythingObserver);
}
