// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ResolvedConstraintScopeExtensions.when_bridging_to_the_legacy_scope_key;

/// <summary>
/// A provider that only implements the original string-keyed member must receive the byte-exact key it used to
/// receive, otherwise the bridge changes its answers instead of preserving them. That holds only if resolving a
/// declaration against an event and then flattening it produces the same text as building the key straight from
/// the same declaration and event - for every declaration, and for whatever the event happens to carry.
/// <para>
/// Every combination of the three declaration dimensions is covered, against events carrying real values, absent
/// values, the concept defaults, empty values and values that contain the characters the key joins on.
/// </para>
/// </summary>
public class and_every_declaration_and_event_combination_is_bridged : Specification
{
    const string Marker = "_scoped_";

    record EventDimensions(string Description, EventSourceType? EventSourceType, EventStreamType? EventStreamType, EventStreamId? EventStreamId);

    record Case(ConstraintScope? Declaration, EventDimensions Event)
    {
        public string ThroughTheBridge => Declaration.ResolveFor(Event.EventSourceType, Event.EventStreamType, Event.EventStreamId).ToLegacyScopeKey();

        public string BuiltDirectly => Declaration.BuildScopeKey(Event.EventSourceType, Event.EventStreamType, Event.EventStreamId);
    }

    static readonly ConstraintScope?[] _declarations =
    [
        null,
        ConstraintScope.None,
        new((EventSourceType)Marker, null, null),
        new(null, (EventStreamType)Marker, null),
        new(null, null, (EventStreamId)Marker),
        new((EventSourceType)Marker, (EventStreamType)Marker, null),
        new((EventSourceType)Marker, null, (EventStreamId)Marker),
        new(null, (EventStreamType)Marker, (EventStreamId)Marker),
        new((EventSourceType)Marker, (EventStreamType)Marker, (EventStreamId)Marker)
    ];

    static readonly EventDimensions[] _events =
    [
        new("all present", "SourceType", "StreamType", "StreamId"),
        new("all absent", null, null, null),
        new("only an event source type", "SourceType", null, null),
        new("only an event stream type", null, "StreamType", null),
        new("only an event stream id", null, null, "StreamId"),
        new("concept defaults", EventSourceType.Default, EventStreamType.All, EventStreamId.Default),
        new("empty values", (EventSourceType)string.Empty, (EventStreamType)string.Empty, (EventStreamId)string.Empty),
        new("values containing the joining character", "loan|estt:branch", "region", "branch|esid:other"),
        new("values containing a part prefix", "loan", "branch|estt:region", "esid:branch")
    ];

    Case[] _cases;
    Case[] _disagreements;

    void Because()
    {
        _cases = [.. from declaration in _declarations
                     from carried in _events
                     select new Case(declaration, carried)];

        _disagreements = [.. _cases.Where(_ => _.ThroughTheBridge != _.BuiltDirectly)];
    }

    [Fact] void should_produce_the_same_key_as_building_it_directly_for_every_combination() => _disagreements.ShouldBeEmpty();
    [Fact] void should_cover_every_combination_of_the_three_declaration_dimensions() => _declarations.Length.ShouldEqual(9);
    [Fact] void should_cover_every_declaration_against_every_event_shape() => _cases.Length.ShouldEqual(_declarations.Length * _events.Length);
    [Fact] void should_have_at_least_one_combination_that_narrows_something() => _cases.ShouldContain(_ => _.BuiltDirectly.Length > 0);
}
