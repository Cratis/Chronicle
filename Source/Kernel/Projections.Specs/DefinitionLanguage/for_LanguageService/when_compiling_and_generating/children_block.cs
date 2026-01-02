// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class children_block : given.a_language_service
{
    const string definition = """
        projection Group => GroupReadModel
          children Members id userId
            automap
            from UserAddedToGroup
              key userId
              parent $eventContext.eventSourceId
              Role = role
            from UserRoleChanged
              key userId
              parent groupId
              Role = role
        """;

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;

    void Because()
    {
        _result = Compile(definition);
        _childrenDef = _result.Children[new PropertyPath("Members")];
    }

    [Fact] void should_have_children_definition() => _childrenDef.ShouldNotBeNull();
    [Fact] void should_have_identifier_expression() => _childrenDef.IdentifiedBy.ShouldNotBeNull();
    [Fact] void should_have_automap_enabled() => _childrenDef.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_two_from_definitions() => _childrenDef.From.Count.ShouldEqual(2);
    [Fact] void should_have_user_added_to_group_event() => _childrenDef.From.ContainsKey((EventType)"UserAddedToGroup").ShouldBeTrue();
    [Fact] void should_have_user_role_changed_event() => _childrenDef.From.ContainsKey((EventType)"UserRoleChanged").ShouldBeTrue();
}
