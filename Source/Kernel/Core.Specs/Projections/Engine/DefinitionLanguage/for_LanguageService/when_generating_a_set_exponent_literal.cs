// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_a_set_exponent_literal : given.a_language_service_with_schemas<given.UserReadModel>
{
    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    string _storedExpression;
    string _generated;

    void Because()
    {
        var result = CompileGenerateAndRecompile("""
            projection User => UserReadModel
              from UserCreated
                set score to 1e-3
            """);
        _storedExpression = result.Definition.From[(EventType)"UserCreated"].Properties[new PropertyPath("score")];
        _generated = result.GeneratedDefinition;
    }

    [Fact] void should_store_the_exponent() => _storedExpression.ShouldEqual("1e-3");
    [Fact] void should_generate_a_valid_exponent() => _generated.ShouldContain("score = 1e-3");
}
