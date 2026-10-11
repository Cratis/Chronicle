// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_a_projection : Specification
{
    ReadModelScenario<ProjectedEventRoute> _scenario;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given.ForEventSource(_source)
            .OnStream(eventSourceType: "Order", eventStreamType: "Payments", eventStreamId: "2026-10")
            .Events(new RoutedEventRecorded("first"), new RoutedEventRecorded("second"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_project_the_source_type() => _scenario.Instance!.SourceType.ShouldEqual((EventSourceType)"Order");
    [Fact] void should_project_the_stream_type() => _scenario.Instance!.StreamType.ShouldEqual((EventStreamType)"Payments");
    [Fact] void should_project_the_stream_id() => _scenario.Instance!.StreamId.ShouldEqual((EventStreamId)"2026-10");
    [Fact] void should_keep_the_event_source_identity() => _scenario.Instance!.Id.ToString().ShouldEqual(_source.Value);
    [Fact] void should_process_both_events_in_order() => _scenario.Instance!.Value.ShouldEqual("second");
    [Fact] void should_keep_the_seeded_position() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)1UL);
}
