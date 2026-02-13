// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class join_block : given.a_language_service_with_schemas<given.UserGroupReadModel>
{
    const string Declaration = """
        projection UserGroups => UserGroupReadModel
          join Group on groupId
            with GroupCreated
              automap
            with GroupRenamed
              automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded), typeof(given.GroupCreated), typeof(given.GroupRenamed)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_two_join_events() => _result.Join.Count.ShouldEqual(2);
    [Fact] void should_have_group_created_join() => _result.Join.ContainsKey((EventType)"GroupCreated").ShouldBeTrue();
    [Fact] void should_have_group_renamed_join() => _result.Join.ContainsKey((EventType)"GroupRenamed").ShouldBeTrue();
    [Fact] void should_have_join_on_group_id() => _result.Join[(EventType)"GroupCreated"].On.ShouldEqual(new PropertyPath("groupId"));
    [Fact] void should_have_automap_enabled() => _result.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_same_on_property_for_second_event() => _result.Join[(EventType)"GroupRenamed"].On.ShouldEqual(new PropertyPath("groupId"));
}
