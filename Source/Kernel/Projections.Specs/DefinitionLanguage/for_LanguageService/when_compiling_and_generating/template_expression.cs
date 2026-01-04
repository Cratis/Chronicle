// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class template_expression : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            fullName = `${firstName} ${lastName}`
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_full_name_property() => _result.From[(EventType)"UserCreated"].Properties.ContainsKey(new PropertyPath("fullName")).ShouldBeTrue();
    [Fact] void should_have_template_expression() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("fullName")].ShouldContain("firstName");
}
