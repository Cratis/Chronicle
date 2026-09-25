// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_EventStoreStatistics.when_folding_rows;

public class and_they_span_namespaces_and_event_types : Specification
{
    EventStoreStatistics _result;

    void Because() => _result = EventStoreStatistics.From(
    [
        new() { EventType = "registered", Namespace = "acme", Count = 3 },
        new() { EventType = "archived", Namespace = "acme", Count = 2 },
        new() { EventType = "registered", Namespace = "globex", Count = 5 }
    ]);

    [Fact] void should_sum_every_row() => _result.TotalEvents.ShouldEqual(10L);
    [Fact] void should_count_an_event_type_once_across_namespaces() => _result.EventTypes.ShouldEqual(2);
    [Fact] void should_count_a_namespace_once_across_event_types() => _result.Namespaces.ShouldEqual(2);
}
