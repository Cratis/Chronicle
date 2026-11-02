// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_appending_after_reading : a_borrowed_store
{
    ReadModelScenario<ScenarioContextReduction> _borrowed;
    ReadModelScenario<ScenarioContextReduction> _fresh;
    int _countBeforeAppending;

    void Establish()
    {
        _borrowed = Borrowed<ScenarioContextReduction>();
        _fresh = Fresh<ScenarioContextReduction>();
    }

    async Task Because()
    {
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            await scenario.ProcessEventsFor(_firstId, [new ScenarioContextRecorded("bare")]);
            await scenario.ProcessAppendedEvents([new AppendedEvent(_context with { SequenceNumber = 9UL }, new ScenarioContextRecorded("later"))]);
        }

        _countBeforeAppending = _borrowed.Instance!.Contexts.Count();
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            await scenario.ProcessAppendedEvents([new AppendedEvent(_context, new ScenarioContextRecorded("first"))]);
        }
    }

    [Fact] void should_fold_the_first_read() => _countBeforeAppending.ShouldEqual(2);
    [Fact] void should_refold_every_collected_event() => _borrowed.Instance!.Contexts.Count().ShouldEqual(3);
    [Fact] void should_match_a_fresh_scenario_apart_from_the_bare_events_clock() => Json(_borrowed.Instance!.Contexts.Select(context => context with { Occurred = DateTimeOffset.MinValue })).ShouldEqual(Json(_fresh.Instance!.Contexts.Select(context => context with { Occurred = DateTimeOffset.MinValue })));
}
