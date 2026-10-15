// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_from_lines_are_repeated : Specification
{
    LegacyEventsSource _result;

    void Because() => _result = LegacyEventsSource.Lift("capture Shipments\r\n  source events\r\n    from ShipmentDispatched\r\n    from ShipmentDispatched # duplicate\r\n  key $eventSourceId");

    [Fact] void should_blank_the_duplicate_without_shifting_line_numbers() => _result.Source.ShouldEqual("capture Shipments\r\n  source events\r\n    from ShipmentDispatched\r\n\r\n  key $eventSourceId");
}
