// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_nested_block.and_generating_it_back;

/// <summary>
/// The Workbench editor loads a definition, generates the declaration, and saves what comes back. With nested
/// blocks missing from the generator that save silently deleted them, and the read model stopped being
/// populated for those properties with nothing to show what happened (#4116).
/// </summary>
public class and_it_is_inside_a_children_block : given.a_language_service_compiling_nested<given.ProjectWithTasksReadModel>
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
    ChildrenDefinition _assigneeNested;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _assigneeNested = _result.Children[(PropertyPath)"tasks"].Nested[(PropertyPath)"assignee"];
    }

    [Fact] void should_still_have_the_nested_assignee() => _result.Children[(PropertyPath)"tasks"].Nested.ContainsKey((PropertyPath)"assignee").ShouldBeTrue();
    [Fact] void should_keep_it_identified_by_nothing() => _assigneeNested.IdentifiedBy.IsSet.ShouldBeFalse();
    [Fact] void should_keep_the_task_assigned_from_event() => _assigneeNested.From.ContainsKey((EventType)"TaskAssigned").ShouldBeTrue();
    [Fact] void should_keep_the_name_mapping() => _assigneeNested.From[(EventType)"TaskAssigned"].Properties[new PropertyPath("name")].ShouldEqual("assigneeName");
    [Fact] void should_keep_the_email_mapping() => _assigneeNested.From[(EventType)"TaskAssigned"].Properties[new PropertyPath("email")].ShouldEqual("assigneeEmail");
    [Fact] void should_keep_the_clear_with() => _assigneeNested.RemovedWith.ContainsKey((EventType)"TaskUnassigned").ShouldBeTrue();
}
