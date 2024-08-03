// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_message_factory : given.a_unique_constraint_builder_with_owner
{
    const string _message = "Some message";

    IConstraintDefinition _result;
    EventType _eventType;

    void Establish()
    {
        _eventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.WithMessage(_ => _message);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_set_message() => _result.MessageCallback(_eventType).Value.ShouldEqual(_message);
}
