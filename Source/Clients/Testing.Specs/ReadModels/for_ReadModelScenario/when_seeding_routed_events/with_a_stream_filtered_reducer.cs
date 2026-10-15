// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_a_stream_filtered_reducer : Specification
{
    ReadModelScenario<StreamFilteredEventRoute> _scenario;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Order", eventStreamType: "Shipping")
            .Events(new RoutedEventRecorded("ignored"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Order", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("first"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("second"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_only_reduce_matching_streams_across_all_sources() => _scenario.Instance!.Count.ShouldEqual(2);
    [Fact] void should_preserve_positions_in_the_unfiltered_history() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)2UL);
}
