// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser.when_parsing_nested_block;

public class inside_a_children_block : given.a_parser
{
    const string Declaration = """
        projection Project => ProjectReadModel
          from ProjectCreated
            Name = name

          children tasks identified by taskId
            from TaskAdded
              key taskId
              parent projectId

            nested assignee
              from TaskAssigned
                Name = assigneeName
              clear with TaskUnassigned
        """;

    ChildrenBlock _children;
    NestedChildBlock _nested;

    void Because()
    {
        Parse(Declaration);
        _children = _document.Projections[0].Directives.OfType<ChildrenBlock>().Single();
        _nested = _children.ChildBlocks.OfType<NestedChildBlock>().Single();
    }

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
    [Fact] void should_have_a_nested_child_block() => _nested.ShouldNotBeNull();
    [Fact] void should_have_the_nested_property_name() => _nested.PropertyName.ShouldEqual("assignee");
    [Fact] void should_have_the_from_event_inside_nested() => _nested.ChildBlocks.OfType<ChildOnEventBlock>().Single().EventType.Name.ShouldEqual("TaskAssigned");
    [Fact] void should_have_the_clear_with_inside_nested() => _nested.ChildBlocks.OfType<ClearWithDirective>().Single().EventType.Name.ShouldEqual("TaskUnassigned");
}
