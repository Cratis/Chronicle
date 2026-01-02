// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DSL;
using Cratis.Chronicle.Projections.DSL.AST;

namespace Cratis.Chronicle.Projections.for_RulesProjectionDslParser;

public class when_parsing_simple_projection : Specification
{
    const string Dsl = "projection MyProjection => Users\n  from UserRegistered\n    key $eventSourceId\n    name = e.name";

    Document _result;

    void Because()
    {
        var tokenizer = new Tokenizer(Dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        _result = parser.Parse();
    }

    [Fact] void should_have_one_projection() => _result.Projections.Count.ShouldEqual(1);
    [Fact] void should_have_projection_name() => _result.Projections[0].Name.ShouldEqual("MyProjection");
    [Fact] void should_have_read_model_type() => _result.Projections[0].ReadModelType.Name.ShouldEqual("Users");
    [Fact] void should_have_one_directive() => _result.Projections[0].Directives.Count.ShouldEqual(1);
    [Fact] void should_have_on_event_block() => _result.Projections[0].Directives[0].ShouldBeOfExactType<FromEventBlock>();
}
