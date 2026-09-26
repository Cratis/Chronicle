// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_removing_a_joined_child;

public class and_the_first_parent_is_written_again : Specification
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
        await _scenario.Given.ForEventSource(_firstProject).Events(new JoinedProjectRegistered("First renamed"));
    }

    [Fact] void should_keep_the_updated_name() => _scenario.InstanceForEventSourceId(_firstProject)!.Name.ShouldEqual("First renamed");
    [Fact] void should_not_restore_the_note_to_the_first_project() => _scenario.InstanceForEventSourceId(_firstProject)!.Notes.ShouldBeEmpty();
    [Fact] void should_leave_the_second_project_without_the_note() => _scenario.InstanceForEventSourceId(_secondProject)!.Notes.ShouldBeEmpty();
}
