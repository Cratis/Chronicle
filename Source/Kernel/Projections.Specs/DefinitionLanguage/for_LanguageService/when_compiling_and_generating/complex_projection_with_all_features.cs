// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class complex_projection_with_all_features : given.a_language_service
{
    const string Definition = """
        projection UserGroup => UserGroupReadModel
          automap

          every
            LastUpdated = $eventContext.occurred
            exclude children

          from GroupCreated
            key $eventSourceId
            Name = name
            Description = description
            CreatedAt = $eventContext.occurred
            MemberCount = 0

          from GroupRenamed
            key $eventSourceId
            Name = newName

          from MemberJoined
            key $eventSourceId
            increment MemberCount

          from MemberLeft
            key $eventSourceId
            decrement MemberCount

          children Members id userId
            automap
            from UserAddedToGroup
              key userId
              parent $eventSourceId
              Role = role
              JoinedAt = $eventContext.occurred
            from UserRoleChanged
              key userId
              parent groupId
              Role = role
            remove with UserRemovedFromGroup key userId
              parent groupId

          join GroupSettings on SettingsId
            events SettingsCreated, SettingsUpdated
            automap
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition, "UserGroupReadModel");

    [Fact] void should_have_multiple_from_events() => _result.From.Count.ShouldBeGreaterThan(2);
    [Fact] void should_have_group_created_event() => _result.From.ContainsKey((EventType)"GroupCreated").ShouldBeTrue();
    [Fact] void should_have_every_definition() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_join_definition() => _result.Join.Count.ShouldEqual(2);
}
