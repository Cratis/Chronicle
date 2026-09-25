// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_removing_a_joined_child;

public class and_two_parents_hold_the_child : Specification
{
    ReadModelScenario<JoinedProjectSummary> _scenario;
    EventSourceId _firstProject;
    EventSourceId _secondProject;
    Guid _noteId;

    void Establish()
    {
        _scenario = new ReadModelScenario<JoinedProjectSummary>();
        _firstProject = EventSourceId.New();
        _secondProject = EventSourceId.New();
        _noteId = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_firstProject).Events(new JoinedProjectRegistered("First"));
        await _scenario.Given.ForEventSource(_secondProject).Events(new JoinedProjectRegistered("Second"));
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new JoinedProjectNoted(_noteId, Guid.Parse(_firstProject.Value), "A"));
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new JoinedProjectNoted(_noteId, Guid.Parse(_secondProject.Value), "B"));
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new JoinedProjectNoteRemovedViaJoin(_noteId));
    }

    [Fact] void should_keep_both_projects() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_remove_the_note_from_the_first_project() => _scenario.InstanceForEventSourceId(_firstProject)!.Notes.ShouldBeEmpty();
    [Fact] void should_remove_the_note_from_the_second_project() => _scenario.InstanceForEventSourceId(_secondProject)!.Notes.ShouldBeEmpty();
}
