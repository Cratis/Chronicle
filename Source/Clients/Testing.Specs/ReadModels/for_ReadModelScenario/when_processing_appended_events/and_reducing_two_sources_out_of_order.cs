// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_reducing_two_sources_out_of_order : appended_contexts
{
    ReadModelScenario<ScenarioContextReduction> _scenario;
    EventContext _later;
    EventContext _other;

    void Establish()
    {
        _scenario = new();
        _later = _context with { SequenceNumber = 9UL, Occurred = _context.Occurred.AddMinutes(1) };
        _other = _context with { EventSourceId = _secondId, SequenceNumber = 5UL };
    }

    Task Because() => _scenario.ProcessAppendedEvents(
    [
        new AppendedEvent(_later, new ScenarioContextRecorded("later")),
        new AppendedEvent(_other, new ScenarioContextRecorded("other")),
        new AppendedEvent(_context, new ScenarioContextRecorded("first"))
    ]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_two_instances() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_reduce_the_first_source_in_sequence_order() => _scenario.InstanceForEventSourceId(_firstId)!.Contexts.ShouldEqual([_context, _later]);
    [Fact] void should_reduce_the_second_source_independently() => _scenario.InstanceForEventSourceId(_secondId)!.Contexts.ShouldEqual([_other]);
    [Fact] void should_refuse_an_ambiguous_single_instance() => Catch.Exception(() => _ = _scenario.Instance).ShouldBeOfExactType<MultipleInstancesMaterialized>();
}
