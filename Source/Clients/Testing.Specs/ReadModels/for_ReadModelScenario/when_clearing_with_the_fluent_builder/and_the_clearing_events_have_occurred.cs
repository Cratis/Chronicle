// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_with_the_fluent_builder;

/// <summary>
/// The formal fluent Clear, everywhere Set reaches: at the root, on a child item, and on a member of a nested
/// object. Clearing a member of the nested object must leave the object itself standing.
/// </summary>
public class and_the_clearing_events_have_occurred : Specification
{
    ReadModelScenario<FluentProjectNotes> _scenario;
    EventSourceId _id;
    Guid _taskId;

    void Establish()
    {
        _id = EventSourceId.New();
        _taskId = Guid.NewGuid();
        _scenario = new ReadModelScenario<FluentProjectNotes>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new ProjectNoted("Check the wiring"),
                new FluentProjectSummarised("Wiring", "Summary note"),
                new FluentProjectTaskAdded(_taskId, "Inspect", "Task note"),
                new ProjectNoteCleared(),
                new FluentProjectSummaryNoteCleared(),
                new FluentProjectTaskNoteCleared(_taskId));

    [Fact] void should_clear_the_root_note() => _scenario.Instance!.Note.ShouldBeNull();
    [Fact] void should_clear_the_nested_note() => _scenario.Instance!.Summary!.Note.ShouldBeNull();
    [Fact] void should_keep_the_nested_object() => _scenario.Instance!.Summary!.Headline.ShouldEqual("Wiring");
    [Fact] void should_clear_the_child_note() => _scenario.Instance!.Tasks.Single().Note.ShouldBeNull();
    [Fact] void should_keep_the_child_item() => _scenario.Instance!.Tasks.Single().Title.ShouldEqual("Inspect");
}
