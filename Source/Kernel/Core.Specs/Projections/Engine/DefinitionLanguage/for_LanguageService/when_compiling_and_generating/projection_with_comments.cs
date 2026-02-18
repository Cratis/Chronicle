// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_with_comments : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        # This is a projection with comments
        projection User => UserReadModel
          # Handle user creation
          from UserAdded
            # Map the name property
            name = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_compile_successfully() => _result.ShouldNotBeNull();
    [Fact] void should_have_user_added_event() => _result.From.ContainsKey((EventType)"UserAdded").ShouldBeTrue();
}
