// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

public class children_with_every_excluding_children : given.a_language_service_with_schemas<given.GroupReadModel>
{
    const string Declaration = """
        projection Group => GroupReadModel
          children members identified by userId
            from UserAddedToGroup
              key userId
              parent $eventContext.eventSourceId
            every
              role = "member"
              exclude children
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAddedToGroup)];

    ChildrenDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration).Definition.Children[new PropertyPath("members")];

    [Fact] void should_exclude_descendant_events() => _result.All.IncludeChildren.ShouldBeFalse();
    [Fact] void should_preserve_mappings() => _result.All.Properties[new PropertyPath("role")].ShouldContain("member");
}
