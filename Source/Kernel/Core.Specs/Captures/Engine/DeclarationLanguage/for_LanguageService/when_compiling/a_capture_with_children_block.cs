// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_capture_with_children_block : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Orders
          source message
            topic products.orders
          key orderId
          children lineItems identified by lineNumber
            append ItemAdded
              when added
              lineNumber = $.lineNumber
            append ItemChanged
              when quantity
              quantity = $.quantity
            append ItemRemoved
              when removed
              lineNumber = $.lineNumber
        """;

    Concepts.Captures.CaptureDefinition _result;

    void Because() => _result = Compile(Declaration);

    [Fact] void should_have_children_scope() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_collection_property() => _result.Children[0].CollectionProperty.ShouldEqual("lineItems");
    [Fact] void should_have_identifier_property() => _result.Children[0].IdentifiedBy.ShouldEqual("lineNumber");
    [Fact] void should_have_three_child_appends() => _result.Children[0].Appends.Count.ShouldEqual(3);
}
