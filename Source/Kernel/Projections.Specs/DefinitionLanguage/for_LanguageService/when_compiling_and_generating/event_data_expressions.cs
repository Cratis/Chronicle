// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class event_data_expressions : given.a_language_service
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            Name = name
            Email = contactInfo.email
            City = address.city
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_three_properties() => _result.From[(EventType)"UserCreated"].Properties.Count.ShouldEqual(3);
    [Fact] void should_have_simple_name_path() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Name")].ShouldEqual("name");
    [Fact] void should_have_nested_email_path() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Email")].ShouldEqual("contactInfo.email");
    [Fact] void should_have_nested_city_path() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("City")].ShouldEqual("address.city");
}
