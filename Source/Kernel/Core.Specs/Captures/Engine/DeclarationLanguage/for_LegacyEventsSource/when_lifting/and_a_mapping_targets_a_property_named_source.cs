// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_a_mapping_targets_a_property_named_source : Specification
{
    LegacyEventsSource _result;

    void Because() => _result = LegacyEventsSource.Lift("capture Shipments\n  source events\n    sequence inbox-fulfillment\n    from ShipmentDispatched\n  key $eventSourceId\n  map\n    source = $.source");

    [Fact] void should_keep_the_lifted_sequence() => _result.Sequence.ShouldEqual("inbox-fulfillment");
    [Fact] void should_leave_the_mapping_untouched() => _result.Source.ShouldEqual("capture Shipments\n  source events\n\n    from ShipmentDispatched\n  key $eventSourceId\n  map\n    source = $.source");
}
