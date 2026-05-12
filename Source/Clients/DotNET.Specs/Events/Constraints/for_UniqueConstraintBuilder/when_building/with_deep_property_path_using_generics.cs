// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_deep_property_path_using_generics : given.a_unique_constraint_builder_with_owner
{
    UniqueConstraintDefinition _result;
    EventType _eventType;

    void Establish()
    {
        _eventType = new EventType(nameof(PersonRegistered), EventTypeGeneration.First);
        _eventTypes.GetSchemaFor(_eventType.Id).Returns(_generator.Generate(typeof(PersonRegistered)));
        _eventTypes.GetEventTypeFor(typeof(PersonRegistered)).Returns(_eventType);

        _constraintBuilder.On<PersonRegistered>(_ => _.Mobile.Number);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_add_event_type() => _result.EventsWithProperties.Single().EventTypeId.ShouldEqual(_eventType.Id);
    [Fact] void should_use_the_full_property_path() => _result.EventsWithProperties.Single().Properties.Single().ShouldEqual("Mobile.Number");
}
