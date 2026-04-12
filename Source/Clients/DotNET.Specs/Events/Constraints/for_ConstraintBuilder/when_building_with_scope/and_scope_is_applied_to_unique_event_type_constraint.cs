// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_with_scope;

public class and_scope_is_applied_to_unique_event_type_constraint : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        const string eventTypeId = nameof(EventWithStringProperty);
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(eventType);

        _constraintBuilder
            .PerEventSourceType()
            .Unique<EventWithStringProperty>();
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_have_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_be_unique_event_type_constraint() => _result[0].ShouldBeOfExactType<UniqueEventTypeConstraintDefinition>();
    [Fact] void should_have_scope_on_constraint() => (_result[0] as UniqueEventTypeConstraintDefinition)!.Scope.ShouldNotBeNull();
    [Fact] void should_have_event_source_type_in_scope() => (_result[0] as UniqueEventTypeConstraintDefinition)!.Scope!.EventSourceType.ShouldNotBeNull();
}
