// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_a_typed_source_and_stream_filter : Specification
{
    ReadModelScenario<TypedFilteredEventRoute> _scenario;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new RoutedEventRecorded("wrong source"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RoutedOrder", eventStreamType: "Shipping")
            .Events(new RoutedEventRecorded("wrong stream"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RoutedOrder", eventStreamType: "Payments", eventStreamId: "2026-10")
            .Events(new RoutedEventRecorded("first"));
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "RoutedOrder", eventStreamType: "Payments", eventStreamId: "2026-11")
            .Events(new RoutedEventRecorded("second"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_require_both_source_and_stream_to_match() => _scenario.Instance!.Count.ShouldEqual(2);
    [Fact] void should_not_filter_stream_identifiers() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)3UL);
}
