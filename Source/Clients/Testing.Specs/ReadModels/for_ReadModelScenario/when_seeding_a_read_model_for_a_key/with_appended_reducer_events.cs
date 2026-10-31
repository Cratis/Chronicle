// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_appended_reducer_events : appended_contexts
{
    ReadModelScenario<ScenarioContextReduction> _scenario;
    EventContext _seedContext;
    EventContext _later;
    EventContext _other;

    void Establish()
    {
        _scenario = new();
        _seedContext = _context with { SequenceNumber = 0UL };
        _later = _context with { SequenceNumber = 9UL, Occurred = _context.Occurred.AddMinutes(1) };
        _other = _context with { EventSourceId = _secondId, SequenceNumber = 5UL };
        _scenario.Given.ForEventSourceId(_firstId).ReadModel(new([_seedContext]));
    }

    Task Because() => _scenario.ProcessAppendedEvents(
    [
        new AppendedEvent(_later, new ScenarioContextRecorded("later")),
        new AppendedEvent(_other, new ScenarioContextRecorded("other")),
        new AppendedEvent(_context, new ScenarioContextRecorded("first"))
    ]);

    void Destroy() => _scenario.Dispose();

    [Fact] void should_retain_the_seeded_position() => _scenario.InstanceForEventSourceId(_firstId)!.Contexts.First().SequenceNumber.ShouldEqual(_seedContext.SequenceNumber);
    [Fact] void should_apply_the_original_contexts_in_order_on_top_of_the_seed() => _scenario.InstanceForEventSourceId(_firstId)!.Contexts.Skip(1).ShouldEqual([_context, _later]);
    [Fact] void should_not_apply_the_seed_to_the_other_source() => _scenario.InstanceForEventSourceId(_secondId)!.Contexts.ShouldEqual([_other]);
}
