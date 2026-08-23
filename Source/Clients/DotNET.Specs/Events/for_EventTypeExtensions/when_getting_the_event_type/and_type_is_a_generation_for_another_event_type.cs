// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_type_is_a_generation_for_another_event_type : Specification
{
    [EventType("my-event", generation: 2)]
    record MyEventV2(string Name);

    [EventTypeGenerationFor<MyEventV2>(1)]
    record MyEventV1(string Name);

    EventType _result;

    void Because() => _result = typeof(MyEventV1).GetEventType();

    [Fact] void should_resolve_the_id_from_the_referenced_type() => _result.Id.Value.ShouldEqual("my-event");
    [Fact] void should_resolve_the_generation_from_the_attribute() => _result.Generation.Value.ShouldEqual(1u);
}
