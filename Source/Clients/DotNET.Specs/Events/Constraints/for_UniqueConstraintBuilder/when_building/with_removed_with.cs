// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_removed_with : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    UniqueConstraintDefinition _result;
    EventType _removedWithEventType;

    void Establish()
    {
        _removedWithEventType = new EventType(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.RemovedWith(_removedWithEventType);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_have_the_removed_with_event_type_id() => _result.RemovedWith.ShouldEqual(_removedWithEventType.Id);
}
