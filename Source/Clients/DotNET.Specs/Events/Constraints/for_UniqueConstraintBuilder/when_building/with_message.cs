// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_message : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    const string _message = "Some message";

    IConstraintDefinition _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(_eventType, EventSequenceNumber.First, "Some Constraint", "Error", []);
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.WithMessage(_message);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_set_message() => _result.MessageCallback(_violation).Value.ShouldEqual(_message);
}
