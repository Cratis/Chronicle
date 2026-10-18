// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_projecting_the_original_context : appended_contexts
{
    ReadModelScenario<ScenarioContextProjection> _scenario;

    void Establish() => _scenario = new();
    Task Because() => _scenario.ProcessAppendedEvents([new AppendedEvent(_context, new ScenarioContextRecorded("original"))]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_preserve_sequence_two() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_preserve_the_occurrence_time_and_offset() => _scenario.Instance!.Occurred.EqualsExact(_context.Occurred).ShouldBeTrue();
    [Fact] void should_preserve_the_source_type() => _scenario.Instance!.SourceType.ShouldEqual(_context.EventSourceType);
    [Fact] void should_preserve_the_stream_type() => _scenario.Instance!.StreamType.ShouldEqual(_context.EventStreamType);
    [Fact] void should_preserve_the_stream_id() => _scenario.Instance!.StreamId.ShouldEqual(_context.EventStreamId);
    [Fact] void should_preserve_the_correlation_id() => _scenario.Instance!.CorrelationId.ShouldEqual(_context.CorrelationId);
    [Fact] void should_resolve_the_source_key() => _scenario.Instance!.Id.ShouldEqual(_firstId.Value);
}
