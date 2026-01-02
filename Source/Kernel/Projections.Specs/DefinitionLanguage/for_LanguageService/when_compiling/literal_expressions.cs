// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class when_compiling_literal_expressions : given.a_language_service
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            Name = name
            IsActive = true
            Status = "Active"
            Version = 1
            Rating = 4.5
            Metadata = null
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_six_properties() => _result.From[(EventType)"UserCreated"].Properties.Count.ShouldEqual(6);
    [Fact] void should_have_name_from_event() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Name")].ShouldEqual("name");
    [Fact] void should_have_boolean_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("IsActive")].ShouldEqual("True");
    [Fact] void should_have_string_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Status")].ShouldContain("Active");
    [Fact] void should_have_integer_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Version")].ShouldEqual("1");
    [Fact] void should_have_decimal_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Rating")].ShouldEqual("4.5");
    [Fact] void should_have_null_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("Metadata")].ShouldEqual("");
}
