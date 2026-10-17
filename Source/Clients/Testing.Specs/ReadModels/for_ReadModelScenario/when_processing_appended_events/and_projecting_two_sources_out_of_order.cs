// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_projecting_two_sources_out_of_order : appended_contexts
{
    ReadModelScenario<ScenarioContextProjection> _scenario;

    void Establish() => _scenario = new();
    Task Because() => _scenario.ProcessAppendedEvents(
    [
        new AppendedEvent(_context with { SequenceNumber = 8UL }, new ScenarioContextRecorded("latest")),
        new AppendedEvent(_context with { EventSourceId = _secondId, SequenceNumber = 5UL }, new ScenarioContextRecorded("other")),
        new AppendedEvent(_context, new ScenarioContextRecorded("first"))
    ]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_two_instances() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_project_the_first_source_in_sequence_order() => _scenario.InstanceForEventSourceId(_firstId)!.Value.ShouldEqual("latest");
    [Fact] void should_preserve_the_latest_position() => _scenario.InstanceForEventSourceId(_firstId)!.Position.ShouldEqual((EventSequenceNumber)8UL);
    [Fact] void should_project_the_second_source_independently() => _scenario.InstanceForEventSourceId(_secondId)!.Value.ShouldEqual("other");
}
