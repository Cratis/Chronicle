// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class from_event_with_mappings : given.a_language_service_with_schemas<given.Model>
{
    const string Declaration = """
        projection Test => Model
          from GenericEvent
            key userId
            name = fullName
            email = emailAddress
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.GenericEvent)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_event_type() => _result.From.ContainsKey((EventType)"GenericEvent").ShouldBeTrue();
    [Fact] void should_have_key() => _result.From[(EventType)"GenericEvent"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"GenericEvent"].Key.Value.ShouldEqual("userId");
    [Fact] void should_have_two_properties() => _result.From[(EventType)"GenericEvent"].Properties.Count.ShouldEqual(2);
    [Fact] void should_map_name() => _result.From[(EventType)"GenericEvent"].Properties["name"].ShouldEqual("fullName");
    [Fact] void should_map_email() => _result.From[(EventType)"GenericEvent"].Properties["email"].ShouldEqual("emailAddress");
}
