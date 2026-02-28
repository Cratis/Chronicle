// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class literal_expressions : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserCreated
            key userId
            name = name
            isActive = true
            status = "Active"
            version = 1
            rating = 4.5
            metadata = null
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_six_properties() => _result.From[(EventType)"UserCreated"].Properties.Count.ShouldEqual(6);
    [Fact] void should_have_name_from_event() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("name")].ShouldEqual("name");
    [Fact] void should_have_boolean_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("isActive")].ShouldEqual("True");
    [Fact] void should_have_string_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("status")].ShouldContain("Active");
    [Fact] void should_have_integer_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("version")].ShouldEqual("1");
    [Fact] void should_have_decimal_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("rating")].ShouldEqual("4.5");
    [Fact] void should_have_null_literal() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("metadata")].ShouldEqual("");
}
