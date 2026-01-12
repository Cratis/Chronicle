// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class remove_with_block : given.a_language_service_with_schemas<given.GroupReadModel>
{
    const string Definition = """
        projection Group => GroupReadModel
          children members identified by userId
            from UserAddedToGroup
              key userId
              parent groupId
            remove with UserRemovedFromGroup key userId
              parent groupId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAddedToGroup), typeof(given.UserRemovedFromGroup)];

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;
    RemovedWithDefinition _removedWithDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Definition);
        _childrenDef = _result.Children[new PropertyPath("members")];
        _removedWithDef = _childrenDef.RemovedWith[(EventType)"UserRemovedFromGroup"];
    }

    [Fact] void should_have_removed_with_definition() => _removedWithDef.ShouldNotBeNull();
    [Fact] void should_have_key() => _removedWithDef.Key.ShouldNotBeNull();
    [Fact] void should_have_parent_key() => _removedWithDef.ParentKey.ShouldNotBeNull();
}
