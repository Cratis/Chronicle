// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block;

public class inside_a_children_block : given.a_language_service_compiling_nested<given.ProjectWithTasksReadModel>
{
    const string Declaration = """
        projection Project => ProjectWithTasksReadModel
          from SliceCreated
            name = name

          children tasks identified by taskId
            from TaskAdded
              key taskId
              parent projectId
              title = title

            nested assignee
              from TaskAssigned
                name = assigneeName
                email = assigneeEmail
              clear with TaskUnassigned
        """;

    protected override IEnumerable<Type> EventTypes =>
        [typeof(given.SliceCreated), typeof(given.TaskAdded), typeof(given.TaskAssigned), typeof(given.TaskUnassigned)];

    ProjectionDefinition _result;
    ChildrenDefinition _tasksChildren;
    ChildrenDefinition _assigneeNested;

    void Because()
    {
        _result = Compile(Declaration);
        _tasksChildren = _result.Children[(PropertyPath)"tasks"];
        _assigneeNested = _tasksChildren.Nested[(PropertyPath)"assignee"];
    }

    [Fact] void should_have_tasks_children() => _result.Children.ContainsKey((PropertyPath)"tasks").ShouldBeTrue();
    [Fact] void should_have_an_assignee_nested_inside_tasks() => _tasksChildren.Nested.ContainsKey((PropertyPath)"assignee").ShouldBeTrue();
    [Fact] void should_have_assignee_identified_by_not_set() => _assigneeNested.IdentifiedBy.IsSet.ShouldBeFalse();
    [Fact] void should_have_the_task_assigned_from_event() => _assigneeNested.From.ContainsKey((EventType)"TaskAssigned").ShouldBeTrue();
    [Fact] void should_have_the_task_unassigned_clear_with() => _assigneeNested.RemovedWith.ContainsKey((EventType)"TaskUnassigned").ShouldBeTrue();
}
