// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_per_value_mode_and_removal_properties : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    UniqueConstraintDefinition _result;

    void Establish()
    {
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(_eventType);
        _constraintBuilder.On<EventWithStringProperty>(_ => _.SomeProperty);
        _constraintBuilder.WithMode(UniqueConstraintMode.PerValue);
        _constraintBuilder.RemovedWith<EventWithStringProperty>(_ => _.SomeProperty);
    }

    void Because() => _result = (UniqueConstraintDefinition)_constraintBuilder.Build();

    [Fact] void should_retain_every_value() => _result.Mode.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_declare_the_removal_event() => _result.RemovedWith.ShouldContainOnly([_eventType.Id]);
    [Fact] void should_carry_the_removal_properties() => _result.RemovalEventDefinitions.Single().Properties.ShouldContainOnly([nameof(EventWithStringProperty.SomeProperty)]);
}
