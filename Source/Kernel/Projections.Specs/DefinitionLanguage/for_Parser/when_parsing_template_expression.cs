// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_template_expression : Specification
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key e.userId
            FullName = `${e.firstName} ${e.lastName}`
        """;

    FromEventBlock _onEvent;
    AssignmentOperation _assignment;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _onEvent = (FromEventBlock)result.Projections[0].Directives[0];
        _assignment = (AssignmentOperation)_onEvent.Mappings[0];
    }

    [Fact] void should_have_assignment() => _assignment.ShouldNotBeNull();
    [Fact] void should_have_template_expression() => _assignment.Value.ShouldBeOfExactType<TemplateExpression>();
    [Fact] void should_have_template_parts() => ((TemplateExpression)_assignment.Value).Parts.Count.ShouldBeGreaterThan(0);
}
