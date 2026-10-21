// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LegacyEventsSource.when_lifting;

public class and_values_contain_quoted_comment_markers : Specification
{
    readonly string[] _values = ["\"shipment#//dispatched\"", "\"shipment\\\"#//dispatched\"", "`shipment#//dispatched`", "`shipment\"#//dispatched`"];
    LegacyEventsSource[] _results;

    void Because() => _results = _values.Select(value => LegacyEventsSource.Lift($"capture Shipments\n  source events\n    sequence {value} // trailing comment\n    from {value} # trailing comment")).ToArray();

    [Fact] void should_preserve_quoted_sequence_values_without_the_comment() => _results.Select(result => result.Sequence).ShouldContainOnly(_values);
    [Fact] void should_preserve_quoted_event_values_without_the_comment() => _results.SelectMany(result => result.Names.Values).ShouldContainOnly(_values);
}
