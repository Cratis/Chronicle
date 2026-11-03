// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_projecting_two_event_sources : a_borrowed_store
{
    ReadModelScenario<ScenarioContextProjection> _borrowed;
    ReadModelScenario<ScenarioContextProjection> _fresh;
    AppendedEvent[] _events;

    void Establish()
    {
        _borrowed = Borrowed<ScenarioContextProjection>();
        _fresh = Fresh<ScenarioContextProjection>();
        _events =
        [
            new(_context with { SequenceNumber = 8UL }, new ScenarioContextRecorded("latest")),
            new(_context with { EventSourceId = _secondId, SequenceNumber = 5UL }, new ScenarioContextRecorded("other")),
            new(_context, new ScenarioContextRecorded("first"))
        ];
    }

    async Task Because()
    {
        await _borrowed.ProcessAppendedEvents(_events);
        await _fresh.ProcessAppendedEvents(_events);
    }

    [Fact] void should_materialize_two_instances() => _borrowed.Instances.Count.ShouldEqual(2);
    [Fact] void should_project_the_first_source_in_sequence_order() => _borrowed.InstanceForEventSourceId(_firstId)!.Value.ShouldEqual("latest");
    [Fact] void should_project_the_second_source_independently() => _borrowed.InstanceForEventSourceId(_secondId)!.Value.ShouldEqual("other");
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowed.Instances).ShouldEqual(Json(_fresh.Instances));
}
