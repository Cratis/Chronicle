// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class children_block : given.a_language_service_with_schemas<given.GroupReadModel>
{
    const string Declaration = """
        projection Group => GroupReadModel
          children members identified by userId
            automap
            from UserAddedToGroup
              key userId
              parent $eventContext.eventSourceId
              role = role
            from UserRoleChanged
              key userId
              parent groupId
              role = role
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAddedToGroup), typeof(given.UserRoleChanged)];

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Declaration);
        _childrenDef = _result.Children[new PropertyPath("members")];
    }

    [Fact] void should_have_children_definition() => _childrenDef.ShouldNotBeNull();
    [Fact] void should_have_identifier_expression() => _childrenDef.IdentifiedBy.ShouldNotBeNull();
    [Fact] void should_have_automap_enabled() => _childrenDef.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_two_from_definitions() => _childrenDef.From.Count.ShouldEqual(2);
    [Fact] void should_have_user_added_to_group_event() => _childrenDef.From.ContainsKey((EventType)"UserAddedToGroup").ShouldBeTrue();
    [Fact] void should_have_user_role_changed_event() => _childrenDef.From.ContainsKey((EventType)"UserRoleChanged").ShouldBeTrue();
}
