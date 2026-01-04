// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_with_automap : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_automap_enabled() => _result.From[(EventType)"UserCreated"].AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_key() => _result.From[(EventType)"UserCreated"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"UserCreated"].Key.Value.ShouldEqual("userId");
}
