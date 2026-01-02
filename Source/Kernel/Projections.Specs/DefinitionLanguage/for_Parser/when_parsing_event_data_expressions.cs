// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_event_data_expressions : Specification
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key e.userId
            Name = e.name
            Email = e.contactInfo.email
            City = e.address.city
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

    [Fact] void should_have_three_mappings() => _onEvent.Mappings.Count.ShouldEqual(3);
    [Fact] void should_have_simple_path() => ((EventDataExpression)((AssignmentOperation)_onEvent.Mappings[0]).Value).Path.ShouldEqual("name");
    [Fact] void should_have_nested_path_for_email() => ((EventDataExpression)((AssignmentOperation)_onEvent.Mappings[1]).Value).Path.ShouldEqual("contactInfo.email");
    [Fact] void should_have_nested_path_for_city() => ((EventDataExpression)((AssignmentOperation)_onEvent.Mappings[2]).Value).Path.ShouldEqual("address.city");
}
