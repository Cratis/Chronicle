// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models.given;

public class a_scenario : Specification
{
    protected EventScenario _scenario;
    protected EventSourceId _parentId;
    protected Guid _firstChildId;
    protected Guid _secondChildId;

    void Establish()
    {
        _scenario = new();
        _parentId = EventSourceId.New();
        _firstChildId = Guid.NewGuid();
        _secondChildId = Guid.NewGuid();
    }

    protected async Task SeedChildren()
    {
        await _scenario.Given.ForEventSource(_parentId).Events(new ProjectOpened("Original"));
        await _scenario.Given.ForEventSource(new EventSourceId(_firstChildId)).Events(new TaskAdded(Guid.Parse(_parentId.Value), _firstChildId, "First"));
        await _scenario.When.ForEventSource(new EventSourceId(_secondChildId)).Events(new TaskAdded(Guid.Parse(_parentId.Value), _secondChildId, "Second"));
    }

    void Destroy() => _scenario.Dispose();

    [EventType]
    public record ProjectOpened(string Name);

    [EventType]
    public record ProjectRenamed(string Name);

    [EventType]
    public record TaskAdded(Guid ProjectId, Guid TaskId, string Title);

    [FromEvent<ProjectOpened>]
    [FromEvent<ProjectRenamed>]
    public record ProjectSummary(Guid Id, string Name);

    [FromEvent<ProjectOpened>]
    public record ProjectWithTasks(
        Guid Id,
        string Name,
        [ChildrenFrom<TaskAdded>(key: nameof(TaskAdded.TaskId), parentKey: nameof(TaskAdded.ProjectId), identifiedBy: nameof(ProjectTask.Id))]
        IEnumerable<ProjectTask> Tasks);

    [FromEvent<TaskAdded>]
    public record ProjectTask(Guid Id, string Title);

    public record ProjectRenameCount(Guid Id, int Count);

    public class CountProjectRenames : IReducerFor<ProjectRenameCount>
    {
        public ProjectRenameCount Rename(ProjectRenamed @event, ProjectRenameCount? current, EventContext context) =>
            new(Guid.Parse(context.EventSourceId.Value), (current?.Count ?? 0) + 1);
    }
}
