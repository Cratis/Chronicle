// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_disposing_scenarios_that_folded_input : a_borrowed_store
{
    ReadModelScenario<Tally> _bare;
    ReadModelScenario<ScenarioContextReduction> _appended;
    ReadModelScenario<Tally> _next;
    string[] _scratchBeforeDispose;
    string[] _scratchAfterDispose;

    void Establish()
    {
        _bare = Borrowed<Tally>();
        _appended = Borrowed<ScenarioContextReduction>();
    }

    async Task Because()
    {
        await _bare.Given.ForEventSource(EventSourceId.New()).Events(new Tallied(), new Tallied());
        _ = _bare.Instance;
        await _appended.ProcessAppendedEvents([new(_context, new ScenarioContextRecorded("first"))]);
        _ = _appended.Instance;
        _scratchBeforeDispose = await ScratchSequences();

        _bare.Dispose();
        _appended.Dispose();
        _scratchAfterDispose = await ScratchSequences();

        _next = Borrowed<Tally>();
        await _next.Given.ForEventSource(EventSourceId.New()).Events(new Tallied(), new Tallied(), new Tallied());
    }

    [Fact] void should_free_appended_input_as_soon_as_it_is_folded() => _scratchBeforeDispose.ShouldNotContain(id => id.StartsWith("testing-appended-input-"));
    [Fact] void should_keep_the_bare_input_until_disposed() => _scratchBeforeDispose.ShouldContain(id => id.StartsWith("testing-read-model-input-"));
    [Fact] void should_free_every_scratch_sequence_on_dispose() => _scratchAfterDispose.ShouldBeEmpty();
    [Fact] void should_leave_the_store_folding_new_scenarios() => _next.Instance!.Count.ShouldEqual(3);

    async Task<string[]> ScratchSequences() =>
        (await _store.TestingStore.Store.GetNamespace(new(_store.TestingStore.Namespace.Value)).GetEventSequences())
            .Select(id => id.Value)
            .Where(id => id.StartsWith("testing-read-model-input-") || id.StartsWith("testing-appended-input-"))
            .ToArray();
}
