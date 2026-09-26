// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_compiling_a_caused_by_dynamic_key : given.a_language_service_with_schemas<given.Model>
{
    const string Declaration = """
        projection Test => Model
          all
            count eventCountByType.$causedBy.userName
        """;

    string _expression;

    void Because() => _expression = CompileGenerateAndRecompile(Declaration).Definition.FromEvery.Properties[new PropertyPath("eventCountByType.$causedBy.userName")];

    [Fact] void should_keep_the_dynamic_key_expression() => _expression.ShouldEqual(WellKnownExpressions.Count);
}
