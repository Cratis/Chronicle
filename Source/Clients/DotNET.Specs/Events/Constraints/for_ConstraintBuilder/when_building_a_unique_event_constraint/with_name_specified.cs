// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_a_unique_event_constraint;

public class with_name_specified : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;
    EventType _eventType;

    void Establish()
    {
        _eventType = new EventType(nameof(SomeEvent), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(SomeEvent)).Returns(_eventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<SomeEvent>(name: "Some name");
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_the_correct_constraint() => _result[0].ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_have_the_correct_name() => _result[0].Name.Value.ShouldEqual("Some name");

    record SomeEvent();
}
