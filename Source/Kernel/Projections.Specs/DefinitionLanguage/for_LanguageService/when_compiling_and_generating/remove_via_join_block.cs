// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class remove_via_join_block : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          children groups id groupId
            from UserAddedToGroup
              parent userId
            join Group on groupId
              events GroupCreated, GroupRenamed
              automap
            remove via join on GroupDeleted
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAddedToGroup), typeof(given.GroupCreated), typeof(given.GroupRenamed), typeof(given.GroupDeleted)];

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;
    RemovedWithJoinDefinition _removedWithJoinDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Definition);
        _childrenDef = _result.Children[new PropertyPath("groups")];
        _removedWithJoinDef = _childrenDef.RemovedWithJoin[(EventType)"GroupDeleted"];
    }

    [Fact] void should_have_removed_with_join_definition() => _removedWithJoinDef.ShouldNotBeNull();
}
