// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_EventTypesForSpecifications.when_constructing;

public class and_event_types_share_an_id_across_generations : Specification
{
    EventTypesForSpecifications _eventTypes;

    void Because() => _eventTypes = new([typeof(Current), typeof(Previous)]);

    [Fact] void should_register_both_clr_types() => _eventTypes.AllClrTypes.ShouldContainOnly(typeof(Current), typeof(Previous));
    [Fact] void should_register_both_generations() => _eventTypes.All.ShouldContainOnly(CurrentEventType, PreviousEventType);
    [Fact] void should_resolve_the_previous_types_generation() => _eventTypes.GetEventTypeFor(typeof(Previous)).ShouldEqual(PreviousEventType);
    [Fact] void should_resolve_the_current_types_generation() => _eventTypes.GetEventTypeFor(typeof(Current)).ShouldEqual(CurrentEventType);
    [Fact] void should_resolve_the_latest_clr_type_by_id() => _eventTypes.GetClrTypeFor(CurrentEventType.Id).ShouldEqual(typeof(Current));
    [Fact] void should_resolve_the_previous_clr_type_by_generation() => _eventTypes.GetClrTypeFor(CurrentEventType.Id, PreviousEventType.Generation).ShouldEqual(typeof(Previous));
    [Fact] void should_resolve_the_current_clr_type_by_generation() => _eventTypes.GetClrTypeFor(CurrentEventType.Id, CurrentEventType.Generation).ShouldEqual(typeof(Current));
    [Fact] void should_fall_back_to_the_latest_clr_type_for_an_unknown_generation() => _eventTypes.GetClrTypeFor(CurrentEventType.Id, 3).ShouldEqual(typeof(Current));
    [Fact] void should_resolve_the_latest_schema_by_id() => _eventTypes.GetSchemaFor(CurrentEventType.Id).Properties.Keys.ShouldContainOnly("name");
    [Fact] void should_recognize_the_shared_event_type_id() => _eventTypes.HasFor(CurrentEventType.Id).ShouldBeTrue();

    static EventType PreviousEventType => new("SpecificationPerson", 1);
    static EventType CurrentEventType => new("SpecificationPerson", 2);

    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string FormerName);

    [EventType("SpecificationPerson", generation: 2)]
    public record Current(string Name);
}
