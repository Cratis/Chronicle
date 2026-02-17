// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class simple_projection : given.a_language_service_with_schemas<given.Users>
{
    const string Declaration = """
        projection MyProjection => Users
          from UserRegistered
            key $eventSourceId
            name = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserRegistered)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_one_from_definition() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_have_from_user_registered() => _result.From.ContainsKey("UserRegistered").ShouldBeTrue();
    [Fact] void should_not_have_key_expression() => _result.From[(EventType)"UserRegistered"].Key.IsSet().ShouldBeFalse();
    [Fact] void should_have_one_property_mapping() => _result.From[(EventType)"UserRegistered"].Properties.Count.ShouldEqual(1);
    [Fact] void should_map_name_property() => _result.From[(EventType)"UserRegistered"].Properties[new PropertyPath("name")].ShouldEqual("name");
}
