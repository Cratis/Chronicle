// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_with_scope;

public class and_no_scoping_options_are_set : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        const string eventTypeId = nameof(EventWithStringProperty);
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(eventType);
        _eventTypes.GetSchemaFor((EventTypeId)eventTypeId).Returns(_generator.Generate(typeof(EventWithStringProperty)));

        _constraintBuilder
            .Unique(b => b.On<EventWithStringProperty>(e => e.SomeProperty));
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_have_one_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_have_no_scope_on_constraint() => (_result[0] as UniqueConstraintDefinition)!.Scope.ShouldBeNull();
}
