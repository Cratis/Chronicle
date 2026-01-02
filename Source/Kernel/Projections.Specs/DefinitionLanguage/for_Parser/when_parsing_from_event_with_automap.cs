// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_from_event_with_automap : Specification
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key e.userId
            automap
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

    [Fact] void should_have_automap_enabled() => _onEvent.AutoMap.ShouldBeTrue();
    [Fact] void should_have_event_type() => _onEvent.EventType.Name.ShouldEqual("UserCreated");
    [Fact] void should_have_key() => _onEvent.Key.ShouldNotBeNull();
}
