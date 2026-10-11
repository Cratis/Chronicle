// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class without_a_route : Specification
{
    ReadModelScenario<ProjectedEventRoute> _scenario;

    void Establish() => _scenario = new();

    async Task Because() =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new RoutedEventRecorded("default"));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_use_the_default_source_type() => _scenario.Instance!.SourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_use_the_all_stream_type() => _scenario.Instance!.StreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_use_the_default_stream_id() => _scenario.Instance!.StreamId.Value.ShouldEqual(EventStreamId.Default);
    [Fact] void should_keep_the_first_seeded_position() => _scenario.Instance!.Position.ShouldEqual(EventSequenceNumber.First);
}
