// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService;

public class when_compiling_remove_via_join_block : given.a_language_service
{
    const string definition = """
        projection User => UserReadModel
          children Groups id groupId
            from UserAddedToGroup
              parent userId
            join Group on GroupId
              events GroupCreated, GroupRenamed
              automap
            remove via join on GroupDeleted
        """;

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;
    RemovedWithJoinDefinition _removedWithJoinDef;

    void Because()
    {
        _result = Compile(definition);
        _childrenDef = _result.Children[new PropertyPath("Groups")];
        _removedWithJoinDef = _childrenDef.RemovedWithJoin[(EventType)"GroupDeleted"];
    }

    [Fact] void should_have_removed_with_join_definition() => _removedWithJoinDef.ShouldNotBeNull();
}
