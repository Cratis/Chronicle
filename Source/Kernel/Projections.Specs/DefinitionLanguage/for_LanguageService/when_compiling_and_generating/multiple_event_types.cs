// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class multiple_event_types : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            name = name
            email = email
            createdAt = $eventContext.occurred
          from UserUpdated
            key userId
            name = name
            email = email
            updatedAt = $eventContext.occurred
          from UserActivated
            key userId
            isActive = true
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated), typeof(given.UserUpdated), typeof(given.UserActivated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_three_event_types() => _result.From.Count.ShouldEqual(3);
    [Fact] void should_have_user_created_event() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_user_updated_event() => _result.From.ContainsKey((EventType)"UserUpdated").ShouldBeTrue();
    [Fact] void should_have_user_activated_event() => _result.From.ContainsKey((EventType)"UserActivated").ShouldBeTrue();
}
