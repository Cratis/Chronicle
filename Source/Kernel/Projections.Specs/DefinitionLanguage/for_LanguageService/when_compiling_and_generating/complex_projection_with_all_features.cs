// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class complex_projection_with_all_features : given.a_language_service_with_schemas<given.UserGroupReadModel>
{
    const string Definition = """
        projection UserGroup => UserGroupReadModel
          automap

          every
            lastUpdated = $eventContext.occurred
            exclude children

          from GroupCreated
            key $eventSourceId
            name = name
            description = description
            createdAt = $eventContext.occurred
            memberCount = 0

          from GroupRenamed
            key $eventSourceId
            name = newName

          from MemberJoined
            key $eventSourceId
            increment memberCount

          from MemberLeft
            key $eventSourceId
            decrement memberCount

          children members identified by userId
            automap
            from UserAddedToGroup
              key userId
              parent $eventSourceId
              role = role
              joinedAt = $eventContext.occurred
            from UserRoleChanged
              key userId
              parent groupId
              role = role
            remove with UserRemovedFromGroup key userId
              parent groupId

          join GroupSettings on settingsId
            with SettingsCreated
              automap
            with SettingsUpdated
              automap
        """;

    protected override IEnumerable<Type> EventTypes => [
        typeof(given.GroupCreated),
        typeof(given.GroupRenamed),
        typeof(given.MemberJoined),
        typeof(given.MemberLeft),
        typeof(given.UserAddedToGroup),
        typeof(given.UserRoleChanged),
        typeof(given.UserRemovedFromGroup),
        typeof(given.SettingsCreated),
        typeof(given.SettingsUpdated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_multiple_from_events() => _result.From.Count.ShouldBeGreaterThan(2);
    [Fact] void should_have_group_created_event() => _result.From.ContainsKey((EventType)"GroupCreated").ShouldBeTrue();
    [Fact] void should_have_every_definition() => _result.FromEvery.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_join_definition() => _result.Join.Count.ShouldEqual(2);
}
