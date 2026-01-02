// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class when_compiling_join_block : given.a_language_service
{
    const string definition = """
        projection UserGroups => UserGroupReadModel
          join Group on GroupId
            events GroupCreated, GroupRenamed
            automap
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_two_join_events() => _result.Join.Count.ShouldEqual(2);
    [Fact] void should_have_group_created_join() => _result.Join.ContainsKey((EventType)"GroupCreated").ShouldBeTrue();
    [Fact] void should_have_group_renamed_join() => _result.Join.ContainsKey((EventType)"GroupRenamed").ShouldBeTrue();
    [Fact] void should_have_join_on_group_id() => _result.Join[(EventType)"GroupCreated"].On.ShouldEqual(new PropertyPath("GroupId"));
    [Fact] void should_have_automap_enabled() => _result.Join[(EventType)"GroupCreated"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_same_on_property_for_second_event() => _result.Join[(EventType)"GroupRenamed"].On.ShouldEqual(new PropertyPath("GroupId"));
}
