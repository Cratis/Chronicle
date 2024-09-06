// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_a_unique_event_constraint;

public class with_message_callback : given.a_constraint_builder_with_owner
{
    const string _message = "Some message";
    IImmutableList<IConstraintDefinition> _result;
    EventTypeId _eventTypeId;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(_eventTypeId, EventSequenceNumber.First, "Some Constraint", "Error", []);
        _eventTypeId = nameof(SomeEvent);
        _eventTypes.GetEventTypeFor(typeof(SomeEvent)).Returns(new EventType(_eventTypeId, EventTypeGeneration.First));
    }

    void Because()
    {
        _constraintBuilder.Unique<SomeEvent>(_ => _message);
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_the_correct_constraint() => _result[0].ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_have_the_correct_name() => _result[0].Name.Value.ShouldEqual(_eventTypeId.Value);
    [Fact] void should_have_the_correct_message() => _result[0].MessageCallback(_violation).Value.ShouldEqual(_message);

    record SomeEvent();
}
