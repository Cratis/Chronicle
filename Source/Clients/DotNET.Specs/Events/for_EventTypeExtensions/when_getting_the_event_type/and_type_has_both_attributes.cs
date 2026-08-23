// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_type_has_both_attributes : Specification
{
    [EventType("my-event", generation: 2)]
    record MyEventV2(string Name);

    [EventType]
    [EventTypeGenerationFor<MyEventV2>(1)]
    record MyEventV1(string Name);

    Exception _error;

    void Because() => _error = Catch.Exception(() => typeof(MyEventV1).GetEventType());

    [Fact] void should_throw_event_type_generation_for_cannot_be_combined_with_event_type() => _error.ShouldBeOfExactType<EventTypeGenerationForCannotBeCombinedWithEventType>();
}
