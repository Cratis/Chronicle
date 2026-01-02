// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_composite_key : Specification
{
    const string definition = """
        projection Order => OrderReadModel
          from OrderCreated
            key OrderKey {
              CustomerId = e.customerId
              OrderNumber = e.orderNumber
            }
            Total = e.total
        """;

    FromEventBlock _onEvent;
    CompositeKeyDirective _compositeKey;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _onEvent = (FromEventBlock)result.Projections[0].Directives[0];
        _compositeKey = _onEvent.CompositeKey!;
    }

    [Fact] void should_have_composite_key() => _compositeKey.ShouldNotBeNull();
    [Fact] void should_have_correct_type_name() => _compositeKey.TypeName.Name.ShouldEqual("OrderKey");
    [Fact] void should_have_two_parts() => _compositeKey.Parts.Count.ShouldEqual(2);
    [Fact] void should_have_customer_id_part() => _compositeKey.Parts[0].PropertyName.ShouldEqual("CustomerId");
    [Fact] void should_have_order_number_part() => _compositeKey.Parts[1].PropertyName.ShouldEqual("OrderNumber");
}
