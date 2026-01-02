// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_qualified_projection_name : Specification
{
    const string Dsl = "projection Core.Simulations.Simulation => Simulation\n  from SimulationAdded\n    key $eventSourceId";

    Document _result;

    void Because()
    {
        var tokenizer = new Tokenizer(Dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        _result = parser.Parse();
    }

    [Fact] void should_have_qualified_projection_name() => _result.Projections[0].Name.ShouldEqual("Core.Simulations.Simulation");
    [Fact] void should_have_read_model_type() => _result.Projections[0].ReadModelType.Name.ShouldEqual("Simulation");
}
