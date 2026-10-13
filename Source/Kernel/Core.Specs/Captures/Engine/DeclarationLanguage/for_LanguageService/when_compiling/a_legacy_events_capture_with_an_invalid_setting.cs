// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_legacy_events_capture_with_an_invalid_setting : given.a_language_service
{
    CompilerErrors _errors;

    void Because() => _errors = _languageService.Compile("""
        capture Shipments
          source events
            sequence inbox-fulfillment
            from ShipmentDispatched
            poll 5m
          key $eventSourceId
          append OrderShipped
            when added
              id = $eventSourceId
        """).Match(_ => CompilerErrors.Empty, errors => errors);

    [Fact] void should_report_errors_on_original_lines() => _errors.Errors.Single().Line.ShouldEqual(5);
}
