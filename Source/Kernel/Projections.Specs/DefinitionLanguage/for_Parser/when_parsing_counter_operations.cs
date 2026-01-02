// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_counter_operations : Specification
{
    const string definition = """
        projection Test => Model
          from UserLoggedIn
            key e.userId
            increment LoginCount
            count EventCount
            decrement RetryCount
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
    [Fact] void should_have_increment_operation() => _onEvent.Mappings[0].ShouldBeOfExactType<IncrementOperation>();
    [Fact] void should_have_count_operation() => _onEvent.Mappings[1].ShouldBeOfExactType<CountOperation>();
    [Fact] void should_have_decrement_operation() => _onEvent.Mappings[2].ShouldBeOfExactType<DecrementOperation>();
    [Fact] void should_have_correct_increment_property_name() => ((IncrementOperation)_onEvent.Mappings[0]).PropertyName.ShouldEqual("LoginCount");
    [Fact] void should_have_correct_count_property_name() => ((CountOperation)_onEvent.Mappings[1]).PropertyName.ShouldEqual("EventCount");
    [Fact] void should_have_correct_decrement_property_name() => ((DecrementOperation)_onEvent.Mappings[2]).PropertyName.ShouldEqual("RetryCount");
}
