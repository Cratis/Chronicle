// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_there_are_several_sequences : Specification
{
    LegacyEventsSource _result;

    void Because() => _result = LegacyEventsSource.Lift("capture Shipments\r\n  source events\r\n    sequence inbox-first\r\n    // a comment\r\n\r\n    sequence inbox-second\r\n    from ShipmentDispatched\r\n  key $eventSourceId");

    [Fact] void should_keep_the_first_sequence_like_the_legacy_visitor() => _result.Sequence.ShouldEqual("inbox-first");
    [Fact] void should_blank_every_sequence_line_without_shifting_line_numbers() => _result.Source.ShouldEqual("capture Shipments\r\n  source events\r\n\r\n    // a comment\r\n\r\n\r\n    from ShipmentDispatched\r\n  key $eventSourceId");
}
