// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class children_with_join : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          children groups id groupId
            from UserAddedToGroup
              key groupId
              parent userId
            join Group on groupId
              events GroupCreated, GroupRenamed
              automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAddedToGroup), typeof(given.GroupCreated), typeof(given.GroupRenamed)];

    ProjectionDefinition _result;
    ChildrenDefinition _childrenDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(Definition);
        _childrenDef = _result.Children[new PropertyPath("groups")];
    }

    [Fact] void should_have_children() => _result.Children.ContainsKey(new PropertyPath("groups")).ShouldBeTrue();
    [Fact] void should_have_two_join_events() => _childrenDef.Join.Count.ShouldEqual(2);
    [Fact] void should_have_group_created_join() => _childrenDef.Join.ContainsKey((EventType)"GroupCreated").ShouldBeTrue();
    [Fact] void should_have_group_renamed_join() => _childrenDef.Join.ContainsKey((EventType)"GroupRenamed").ShouldBeTrue();
    [Fact] void should_have_join_on_group_id() => _childrenDef.Join[(EventType)"GroupCreated"].On.ShouldEqual(new PropertyPath("groupId"));
    [Fact] void should_have_automap_enabled() => _childrenDef.Join[(EventType)"GroupCreated"].AutoMap.ShouldEqual(AutoMap.Inherit);
}
