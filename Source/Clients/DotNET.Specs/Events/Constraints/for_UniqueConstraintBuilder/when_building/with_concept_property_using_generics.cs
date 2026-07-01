// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_concept_property_using_generics : given.a_unique_constraint_builder_with_owner
{
    UniqueConstraintDefinition _result;
    Exception _error;
    EventType _eventType;

    void Establish()
    {
        _eventType = new EventType(nameof(AccountRegistered), EventTypeGeneration.First);
        _eventTypes.GetSchemaFor(_eventType.Id).Returns(_generator.Generate(typeof(AccountRegistered)));
        _eventTypes.GetEventTypeFor(typeof(AccountRegistered)).Returns(_eventType);
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _constraintBuilder.On<AccountRegistered>(_ => _.Email);
        _result = _constraintBuilder.Build() as UniqueConstraintDefinition;
    });

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_add_event_type() => _result.EventsWithProperties.Single().EventTypeId.ShouldEqual(_eventType.Id);
    [Fact] void should_use_the_concept_property_as_a_single_leaf() => _result.EventsWithProperties.Single().Properties.Single().ShouldEqual("Email");
}
