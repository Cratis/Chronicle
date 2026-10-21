// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_the_original_builder_reused : Specification
{
    ReadModelScenario<ProjectedEventRoute> _scenario;
    ReadModelSourceGivenBuilder<ProjectedEventRoute> _builder;
    ProjectedEventRoute _routed;

    void Establish()
    {
        _scenario = new();
        _builder = _scenario.Given.ForEventSource(EventSourceId.New());
    }

    async Task Because()
    {
        await _builder.OnStream("Order", "Payments", "2026-10").Events(new RoutedEventRecorded("routed"));
        _routed = _scenario.Instance!;
        await _builder.Events(new RoutedEventRecorded("unrouted"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_preserve_the_routed_seed() => _routed.StreamId.Value.ShouldEqual("2026-10");
    [Fact] void should_not_change_the_original_source_type() => _scenario.Instance!.SourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_not_change_the_original_stream_type() => _scenario.Instance!.StreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_not_change_the_original_stream_id() => _scenario.Instance!.StreamId.Value.ShouldEqual(EventStreamId.Default);
    [Fact] void should_refold_the_accumulated_history() => _scenario.Instance!.Value.ShouldEqual("unrouted");
}
