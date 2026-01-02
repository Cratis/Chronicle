// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_multiple_event_types : Specification
{
    const string definition = """
        projection User => UserReadModel
          from UserCreated
            key e.userId
            Name = e.name
            Email = e.email
            CreatedAt = ctx.occurred
          from UserUpdated
            key e.userId
            Name = e.name
            Email = e.email
            UpdatedAt = ctx.occurred
          from UserActivated
            key e.userId
            IsActive = true
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

    [Fact] void should_have_three_event_blocks() => _result.Projections[0].Directives.Count.ShouldEqual(3);
    [Fact] void should_have_user_created_event() => ((FromEventBlock)_result.Projections[0].Directives[0]).EventType.Name.ShouldEqual("UserCreated");
    [Fact] void should_have_user_updated_event() => ((FromEventBlock)_result.Projections[0].Directives[1]).EventType.Name.ShouldEqual("UserUpdated");
    [Fact] void should_have_user_activated_event() => ((FromEventBlock)_result.Projections[0].Directives[2]).EventType.Name.ShouldEqual("UserActivated");
}
