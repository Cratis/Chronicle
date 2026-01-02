// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_arithmetic_operations : Specification
{
    const string definition = """
        projection Account => AccountReadModel
          from MoneyDeposited
            key e.accountId
            add Balance by e.amount
          from MoneyWithdrawn
            key e.accountId
            subtract Balance by e.amount
        """;

    FromEventBlock _depositEvent;
    FromEventBlock _withdrawEvent;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _depositEvent = (FromEventBlock)result.Projections[0].Directives[0];
        _withdrawEvent = (FromEventBlock)result.Projections[0].Directives[1];
    }

    [Fact] void should_have_add_operation_in_deposit() => _depositEvent.Mappings[0].ShouldBeOfExactType<AddOperation>();
    [Fact] void should_have_subtract_operation_in_withdraw() => _withdrawEvent.Mappings[0].ShouldBeOfExactType<SubtractOperation>();
    [Fact] void should_have_correct_add_property_name() => ((AddOperation)_depositEvent.Mappings[0]).PropertyName.ShouldEqual("Balance");
    [Fact] void should_have_correct_subtract_property_name() => ((SubtractOperation)_withdrawEvent.Mappings[0]).PropertyName.ShouldEqual("Balance");
    [Fact] void should_have_add_value_expression() => ((AddOperation)_depositEvent.Mappings[0]).Value.ShouldNotBeNull();
    [Fact] void should_have_subtract_value_expression() => ((SubtractOperation)_withdrawEvent.Mappings[0]).Value.ShouldNotBeNull();
}
