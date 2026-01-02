// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class from_event_with_mappings : given.a_language_service
{
    const string definition = """
        projection Test => Model
          from EventType
            key userId
            name = fullName
            email = emailAddress
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_from_event_type() => _result.From.ContainsKey((EventType)"EventType").ShouldBeTrue();
    [Fact] void should_have_key() => _result.From[(EventType)"EventType"].Key.ShouldNotBeNull();
    [Fact] void should_have_key_value() => _result.From[(EventType)"EventType"].Key.Value.ShouldEqual("userId");
    [Fact] void should_have_two_properties() => _result.From[(EventType)"EventType"].Properties.Count.ShouldEqual(2);
    [Fact] void should_map_name() => _result.From[(EventType)"EventType"].Properties[new PropertyPath("name")].ShouldEqual("fullName");
    [Fact] void should_map_email() => _result.From[(EventType)"EventType"].Properties[new PropertyPath("email")].ShouldEqual("emailAddress");
}
