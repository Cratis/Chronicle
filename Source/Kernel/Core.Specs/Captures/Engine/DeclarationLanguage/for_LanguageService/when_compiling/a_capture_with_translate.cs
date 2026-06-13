// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_capture_with_translate : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Customers
          source api
            api CustomersApi
            poll 5m
          key customerId
          map
            status = status translate
              "aktiv" => active
              "inaktiv" => inactive
          append CustomerChanged
            when status
            status = $.status
        """;

    CaptureDefinition _result;
    TranslateOperation _operation;

    void Because()
    {
        _result = Compile(Declaration);
        _operation = (TranslateOperation)_result.Map!.Operations[0];
    }

    [Fact] void should_have_translate_operation() => _operation.TargetProperty.ShouldEqual("status");
    [Fact] void should_have_source_property() => _operation.SourceProperty.ShouldEqual("status");
    [Fact] void should_have_first_translation() => _operation.Translations[0].From.ShouldEqual("aktiv");
    [Fact] void should_have_second_translation_target() => _operation.Translations[1].To.ShouldEqual("inactive");
}
