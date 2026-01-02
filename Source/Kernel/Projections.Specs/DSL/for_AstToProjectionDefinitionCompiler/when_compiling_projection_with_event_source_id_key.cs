// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.LanguageDefinition;

namespace Cratis.Chronicle.Projections.for_AstToProjectionDefinitionCompiler;

public class when_compiling_projection_with_event_source_id_key : Specification
{
    const string Dsl = "projection Test => Model\n  from EventType\n    key $eventSourceId\n    name = e.name";

    ProjectionDefinition _result;

    void Because()
    {
        var tokenizer = new Tokenizer(Dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var document = parser.Parse();
        var compiler = new Compiler();
        _result = compiler.Compile(document, new ProjectionId("test"), ProjectionOwner.Client, EventSequenceId.Log);
    }

    [Fact] void should_have_event_source_id_as_key() => _result.From.Values.First().Key.Value.ShouldEqual("$eventSourceId");
}
