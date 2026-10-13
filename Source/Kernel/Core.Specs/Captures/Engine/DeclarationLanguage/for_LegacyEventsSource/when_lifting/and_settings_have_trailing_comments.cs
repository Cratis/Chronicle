// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_settings_have_trailing_comments : Specification
{
    LegacyEventsSource[] _results;

    void Because() => _results = new[] { "//", "#" }.Select(marker => LegacyEventsSource.Lift($"capture Shipments\r\n  source events\r\n    sequence inbox-fulfillment {marker} sequence comment\r\n    from shipment-dispatched {marker} event comment\r\n  key $eventSourceId")).ToArray();

    [Fact] void should_not_include_comments_in_the_sequence() => _results.Select(result => result.Sequence).ShouldContainOnly("inbox-fulfillment", "inbox-fulfillment");
    [Fact] void should_not_include_comments_in_the_event_name() => _results.SelectMany(result => result.Names.Values).ShouldContainOnly("shipment-dispatched", "shipment-dispatched");
    [Fact] void should_retain_line_positions() => _results.Select(result => result.Source.Split('\n').Length).ShouldContainOnly(5, 5);
}
