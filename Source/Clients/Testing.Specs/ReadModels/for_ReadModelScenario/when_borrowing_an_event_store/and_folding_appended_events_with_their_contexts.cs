// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_folding_appended_events_with_their_contexts : a_borrowed_store
{
    ReadModelScenario<ScenarioContextReduction> _borrowed;
    ReadModelScenario<ScenarioContextReduction> _fresh;
    AppendedEvent[] _events;

    void Establish()
    {
        _borrowed = Borrowed<ScenarioContextReduction>();
        _fresh = Fresh<ScenarioContextReduction>();
        _events =
        [
            new(_context with { SequenceNumber = 9UL, Tags = [new Tag("later")] }, new ScenarioContextRecorded("later")),
            new(_context, new ScenarioContextRecorded("first"))
        ];
    }

    async Task Because()
    {
        await _borrowed.ProcessAppendedEvents(_events);
        await _fresh.ProcessAppendedEvents(_events);
    }

    [Fact] void should_fold_in_sequence_order() => _borrowed.Instance!.Contexts.Select(context => context.SequenceNumber.Value).ShouldEqual([2UL, 9UL]);
    [Fact] void should_preserve_the_original_context() => _borrowed.Instance!.Contexts.First().ShouldEqual(_context);
    [Fact] void should_preserve_the_tags_in_order() => _borrowed.Instance!.Contexts.Select(context => context.Tags.Single().Value).ShouldEqual(["context-tag", "later"]);
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowed.Instance).ShouldEqual(Json(_fresh.Instance));
}
