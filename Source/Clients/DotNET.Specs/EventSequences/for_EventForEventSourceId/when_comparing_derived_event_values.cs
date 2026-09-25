// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_comparing_derived_event_values : Specification
{
    EventForEventSourceId _base;
    RoutedEvent _implicit;
    RoutedEvent _explicit;
    RoutedEvent _different;
    HashSet<EventForEventSourceId> _values;

    void Establish()
    {
        var payload = new object();
        _base = new("source", payload);
        _implicit = new("source", payload, "route");
        _explicit = _implicit with
        {
            EventSourceType = EventSourceType.Default,
            EventStreamType = EventStreamType.All,
            EventStreamId = EventStreamId.Default
        };
        _different = _implicit with { Route = "other" };
    }

    void Because() => _values = [_implicit, _explicit, _different];

    [Fact] void should_not_equate_the_base_and_derived_records() => _base.Equals(_implicit).ShouldBeFalse();
    [Fact] void should_not_equate_the_derived_and_base_records() => _implicit.Equals(_base).ShouldBeFalse();
    [Fact] void should_compare_derived_values() => _implicit.Equals(_different).ShouldBeFalse();
    [Fact] void should_preserve_default_route_equality_for_derived_records() => _implicit.Equals(_explicit).ShouldBeTrue();
    [Fact] void should_preserve_matching_hash_codes_for_derived_records() => _implicit.GetHashCode().ShouldEqual(_explicit.GetHashCode());
    [Fact] void should_compare_equal_values_through_base_references() => ((EventForEventSourceId)_implicit).Equals(_explicit).ShouldBeTrue();
    [Fact] void should_compare_different_values_through_base_references() => ((EventForEventSourceId)_implicit).Equals(_different).ShouldBeFalse();
    [Fact] void should_compare_equal_values_through_object_references() => ((object)_implicit).Equals(_explicit).ShouldBeTrue();
    [Fact] void should_compare_different_values_through_object_references() => ((object)_implicit).Equals(_different).ShouldBeFalse();
    [Fact] void should_deduplicate_only_equal_derived_values() => _values.Count.ShouldEqual(2);

    record RoutedEvent(EventSourceId EventSourceId, object Event, string Route) : EventForEventSourceId(EventSourceId, Event);
}
