// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_bare_events_were_already_collected : appended_contexts
{
    ReadModelScenario<ScenarioContextReduction> _scenario;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.ProcessEventsFor(_firstId, [new ScenarioContextRecorded("bare")]);
        _ = _scenario.Instance;
    }

    Task Because() => _scenario.ProcessAppendedEvents([new AppendedEvent(_context, new ScenarioContextRecorded("appended"))]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_keep_both_events() => _scenario.Instance!.Contexts.Count().ShouldEqual(2);
    [Fact] void should_keep_the_bare_event_synthetic_position() => _scenario.Instance!.Contexts.First().SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_preserve_the_appended_context() => _scenario.Instance!.Contexts.Last().ShouldEqual(_context);
}
