// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_with_the_fluent_builder;

/// <summary>
/// The control for every fluent clear. Without it a null note at any of the three levels would prove nothing - a
/// projection that never wrote the note would pass the clearing spec just as well.
/// </summary>
public class and_the_clearing_events_have_not_occurred : Specification
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
                new FluentProjectTaskAdded(_taskId, "Inspect", "Task note"));

    [Fact] void should_hold_the_root_note() => _scenario.Instance!.Note.ShouldEqual("Check the wiring");
    [Fact] void should_hold_the_nested_note() => _scenario.Instance!.Summary!.Note.ShouldEqual("Summary note");
    [Fact] void should_hold_the_child_note() => _scenario.Instance!.Tasks.Single().Note.ShouldEqual("Task note");
}
