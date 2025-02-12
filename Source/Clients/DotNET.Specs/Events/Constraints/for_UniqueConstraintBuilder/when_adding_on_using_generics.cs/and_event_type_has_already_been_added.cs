// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_generics;

public class and_event_type_has_already_been_added : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    EventTypeAlreadyAddedToUniqueConstraint _result;

    void Establish()
    {
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(new EventType(nameof(EventWithStringProperty), EventTypeGeneration.First));
        _constraintBuilder.On<EventWithStringProperty>(e => e.SomeProperty);
    }

    void Because() => _result = Catch.Exception<EventTypeAlreadyAddedToUniqueConstraint>(() => _constraintBuilder.On<EventWithStringProperty>(e => e.SomeProperty));

    [Fact] void should_throw_event_type_already_added_to_unique_constraint() => _result.ShouldNotBeNull();
}
