// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_concept_properties_across_multiple_event_types : given.a_unique_constraint_builder_with_owner
{
    UniqueConstraintDefinition _result;
    Exception _error;
    EventType _registeredEventType;
    EventType _claimedEventType;

    void Establish()
    {
        _registeredEventType = new EventType(nameof(AccountRegistered), EventTypeGeneration.First);
        _claimedEventType = new EventType(nameof(AccountReferenceClaimed), EventTypeGeneration.First);

        _eventTypes.GetSchemaFor(_registeredEventType.Id).Returns(_generator.Generate(typeof(AccountRegistered)));
        _eventTypes.GetSchemaFor(_claimedEventType.Id).Returns(_generator.Generate(typeof(AccountReferenceClaimed)));
        _eventTypes.GetEventTypeFor(typeof(AccountRegistered)).Returns(_registeredEventType);
        _eventTypes.GetEventTypeFor(typeof(AccountReferenceClaimed)).Returns(_claimedEventType);
    }

    void Because() => _error = Catch.Exception(() =>
    {
        _constraintBuilder
            .On<AccountRegistered>(_ => _.Reference)
            .On<AccountReferenceClaimed>(_ => _.Reference);
        _result = _constraintBuilder.Build() as UniqueConstraintDefinition;
    });

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_add_both_event_types() => _result.EventsWithProperties.Select(_ => _.EventTypeId).ShouldContainOnly(_registeredEventType.Id, _claimedEventType.Id);
    [Fact] void should_use_the_guid_concept_property_on_the_registered_event() => _result.EventsWithProperties.Single(_ => _.EventTypeId == _registeredEventType.Id).Properties.Single().ShouldEqual("Reference");
    [Fact] void should_use_the_guid_concept_property_on_the_claimed_event() => _result.EventsWithProperties.Single(_ => _.EventTypeId == _claimedEventType.Id).Properties.Single().ShouldEqual("Reference");
}
