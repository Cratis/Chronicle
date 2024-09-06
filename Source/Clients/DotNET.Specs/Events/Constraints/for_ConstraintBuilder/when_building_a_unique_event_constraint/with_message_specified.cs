// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Integration;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_a_unique_event_constraint;

public class with_message_specified : given.a_constraint_builder_with_owner
{
    const string _message = "Some message";
    IImmutableList<IConstraintDefinition> _result;
    EventType _eventType;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(_eventType, EventSequenceNumber.First, "Some Constraint", "Error", []);
        _eventType = new EventType(nameof(SomeEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(SomeEvent)).Returns(_eventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<SomeEvent>(message: _message);
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_the_correct_constraint() => _result[0].ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_have_the_correct_name() => _result[0].Name.Value.ShouldEqual(_eventType.Id.Value);
    [Fact] void should_have_the_correct_message() => _result[0].MessageCallback(_violation).Value.ShouldEqual(_message);

    record SomeEvent();
}
