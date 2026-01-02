// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_literal_expressions : Specification
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key e.userId
            Name = e.name
            IsActive = true
            Status = "Active"
            Version = 1
            Rating = 4.5
            Metadata = null
        """;

    FromEventBlock _onEvent;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _onEvent = (FromEventBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_six_mappings() => _onEvent.Mappings.Count.ShouldEqual(6);
    [Fact] void should_have_boolean_literal() => ((LiteralExpression)((AssignmentOperation)_onEvent.Mappings[1]).Value).Value.ShouldEqual(true);
    [Fact] void should_have_string_literal() => ((LiteralExpression)((AssignmentOperation)_onEvent.Mappings[2]).Value).Value.ShouldEqual("Active");
    [Fact] void should_have_integer_literal() => ((LiteralExpression)((AssignmentOperation)_onEvent.Mappings[3]).Value).Value.ShouldEqual(1.0);
    [Fact] void should_have_decimal_literal() => ((LiteralExpression)((AssignmentOperation)_onEvent.Mappings[4]).Value).Value.ShouldEqual(4.5);
    [Fact] void should_have_null_literal() => ((LiteralExpression)((AssignmentOperation)_onEvent.Mappings[5]).Value).Value.ShouldBeNull();
}
