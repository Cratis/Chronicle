// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class template_expression : given.a_language_service
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key userId
            FullName = `${firstName} ${lastName}`
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_from_user_created() => _result.From.ContainsKey((EventType)"UserCreated").ShouldBeTrue();
    [Fact] void should_have_full_name_property() => _result.From[(EventType)"UserCreated"].Properties.ContainsKey(new PropertyPath("FullName")).ShouldBeTrue();
    [Fact] void should_have_template_expression() => _result.From[(EventType)"UserCreated"].Properties[new PropertyPath("FullName")].ShouldContain("firstName");
}
