// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_more_events_are_collected_after_reading : appended_contexts
{
    ReadModelScenario<ScenarioContextReduction> _scenario;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.ProcessAppendedEvents([new AppendedEvent(_context with { SequenceNumber = 9UL }, new ScenarioContextRecorded("later"))]);
        _ = _scenario.Instance;
    }

    Task Because() => _scenario.ProcessAppendedEvents([new AppendedEvent(_context, new ScenarioContextRecorded("first"))]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_rebuild_all_collected_events_in_sequence_order() => _scenario.Instance!.Contexts.Select(context => context.SequenceNumber.Value).ShouldEqual([2UL, 9UL]);
    [Fact] void should_preserve_the_original_context_after_rebuilding() => _scenario.Instance!.Contexts.First().ShouldEqual(_context);
}
