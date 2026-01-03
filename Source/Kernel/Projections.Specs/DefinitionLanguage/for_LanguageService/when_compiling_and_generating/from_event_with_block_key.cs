// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_with_block_key : given.a_language_service
{
    const string Definition = """
        projection User => UserReadModel
          from UserAssignedToGroup
            key userId
            automap
            GroupId = $eventContext.eventSourceId
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "UserReadModel");

    [Fact] void should_have_from_user_assigned_to_group() => _result.From.ContainsKey((EventType)"UserAssignedToGroup").ShouldBeTrue();
    [Fact] void should_have_automap_enabled() => _result.From[(EventType)"UserAssignedToGroup"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_key() => _result.From[(EventType)"UserAssignedToGroup"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"UserAssignedToGroup"].Key.Value.ShouldEqual("userId");
    [Fact] void should_have_group_id_property() => _result.From[(EventType)"UserAssignedToGroup"].Properties.ContainsKey(new PropertyPath("GroupId")).ShouldBeTrue();
    [Fact] void should_map_group_id_to_event_source_id() => _result.From[(EventType)"UserAssignedToGroup"].Properties[new PropertyPath("GroupId")].ShouldEqual("$eventContext(eventSourceId)");
}
