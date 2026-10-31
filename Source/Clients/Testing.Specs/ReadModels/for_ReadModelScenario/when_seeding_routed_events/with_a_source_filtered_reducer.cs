// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_a_source_filtered_reducer : Specification
{
    ReadModelScenario<SourceFilteredEventRoute> _scenario;
    EventSourceId _matchingSource;
    EventSourceId _otherSource;

    void Establish()
    {
        _scenario = new();
        _matchingSource = EventSourceId.New();
        _otherSource = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_otherSource)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("ignored"));
        await _scenario.Given.ForEventSource(_matchingSource)
            .OnStream(eventSourceType: "Order", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("first"));
        await _scenario.Given.ForEventSource(_matchingSource)
            .OnStream(eventSourceType: "Order", eventStreamType: "Shipping")
            .Events(new RoutedEventRecorded("second"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_only_reduce_matching_sources_across_all_streams() => _scenario.Instance!.Count.ShouldEqual(2);
    [Fact] void should_preserve_positions_in_the_unfiltered_history() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_key_the_result_by_the_matching_source() => _scenario.InstanceForEventSourceId(_matchingSource)!.Count.ShouldEqual(2);
    [Fact] void should_not_materialize_the_other_source() => _scenario.InstanceForEventSourceId(_otherSource).ShouldBeNull();
}
