// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService;

public class when_compiling_qualified_projection_name : given.a_language_service
{
    const string definition = """
        projection Core.Simulations.Simulation => Simulation
          from SimulationAdded
            key $eventSourceId
        """;

    Document _result;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        _result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
    }

    [Fact] void should_have_qualified_projection_name() => _result.Projections[0].Name.ShouldEqual("Core.Simulations.Simulation");
    [Fact] void should_have_read_model_type() => _result.Projections[0].ReadModelType.Name.ShouldEqual("Simulation");
}
