// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class an_events_capture_with_an_unsupported_expression : given.a_language_service
{
    CompilerErrors _errors;
    Exception? _exception;

    void Because() => _exception = Catch.Exception(() => _errors = _languageService.Compile("""
        capture Shipments
          source events
            from ShipmentDispatched
          key $eventSourceId
          append OrderShipped
            when added
              by = $identity.name
        """).Match(_ => CompilerErrors.Empty, errors => errors));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_report_a_compiler_error() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_the_expression_location() => _errors.Errors.Single().Line.ShouldEqual(7);
}
