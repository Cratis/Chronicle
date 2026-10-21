// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_legacy_events_capture_with_header_whitespace_and_comments : given.a_language_service
{
    readonly string[] _headers = ["source  events", "source\tevents", "source events // comment", "source events # comment"];
    CaptureDefinition[] _results;

    void Because() => _results = _headers.Select(header => Compile($"capture Shipments\n  {header}\n    sequence inbox-fulfillment\n    from ShipmentDispatched\n  key $eventSourceId")).ToArray();

    [Fact] void should_lift_each_legacy_sequence() => _results.Select(result => result.Source.Sequence).ShouldContainOnly(_headers.Select(_ => "inbox-fulfillment"));
    [Fact] void should_keep_each_event() => _results.SelectMany(result => result.Source.Events!).ShouldContainOnly(_headers.Select(_ => "ShipmentDispatched"));
}
