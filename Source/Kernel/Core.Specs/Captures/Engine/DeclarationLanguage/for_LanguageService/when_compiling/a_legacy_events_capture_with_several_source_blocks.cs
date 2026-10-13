// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_legacy_events_capture_with_several_source_blocks : given.a_language_service
{
    CaptureDefinition[] _results;

    void Because() => _results = new[] { "    sequence inbox-second\n    sequence inbox-ignored\n", string.Empty }.Select(sequence => Compile($"capture Shipments\n  source events\n    sequence inbox-first\n    from ShipmentDispatched\n  source events\n{sequence}    from ShipmentDispatched\n  key $eventSourceId")).ToArray();

    [Fact] void should_keep_the_first_sequence_from_the_last_block() => _results[0].Source.Sequence.ShouldEqual("inbox-second");
    [Fact] void should_not_carry_the_sequence_from_an_earlier_block() => _results[1].Source.Sequence.ShouldBeNull();
    [Fact] void should_not_deduplicate_from_lines_across_blocks() => _results.All(result => result.Source.Events!.SequenceEqual(["ShipmentDispatched"])).ShouldBeTrue();
}
