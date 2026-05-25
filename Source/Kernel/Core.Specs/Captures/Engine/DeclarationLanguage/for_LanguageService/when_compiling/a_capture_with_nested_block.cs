// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_capture_with_nested_block : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Customers
          source webhook
            path /customers
            auth bearer $env.WebhookToken
          key customerId
          nested billingAddress
            map
              street = fakturaAdresse.gate
            append BillingAddressChanged
              when street
              street = $.street
        """;

    Concepts.Captures.CaptureDefinition _result;

    void Because() => _result = Compile(Declaration);

    [Fact] void should_have_nested_scope() => _result.Nested.Count.ShouldEqual(1);
    [Fact] void should_have_nested_object_path() => _result.Nested[0].ObjectPath.ShouldEqual("billingAddress");
    [Fact] void should_have_nested_map_operation() => ((Concepts.Captures.FieldRenameOperation)_result.Nested[0].Map!.Operations[0]).TargetProperty.ShouldEqual("street");
    [Fact] void should_have_nested_append() => _result.Nested[0].Appends[0].EventType.ShouldEqual("BillingAddressChanged");
}
