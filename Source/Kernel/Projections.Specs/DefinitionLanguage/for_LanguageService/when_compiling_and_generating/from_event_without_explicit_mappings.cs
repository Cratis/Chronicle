// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_without_explicit_mappings : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserCreated key userId
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_user_created() => _result.Definition.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_default_to_automap_enabled() => _result.Definition.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_have_key() => _result.Definition.From[(EventType)"UserCreated"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.Definition.From[(EventType)"UserCreated"].Key.Value.ShouldEqual("userId");
    [Fact] void should_not_have_any_property_mappings() => _result.Definition.From[(EventType)"UserCreated"].Properties.Count.ShouldEqual(0);
    [Fact] void should_not_generate_name_mapping() => _result.GeneratedDefinition.ShouldNotContain("name = name");
    [Fact] void should_not_generate_age_mapping() => _result.GeneratedDefinition.ShouldNotContain("age = age");
    [Fact] void should_not_generate_email_mapping() => _result.GeneratedDefinition.ShouldNotContain("email = email");
}
