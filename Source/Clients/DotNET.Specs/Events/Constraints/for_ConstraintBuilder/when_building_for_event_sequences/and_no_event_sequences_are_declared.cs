// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_for_event_sequences;

/// <summary>
/// No declaration keeps the behavior every constraint had before it could be narrowed: it applies to every event
/// sequence, which the definition expresses as no event sequences at all.
/// </summary>
public class and_no_event_sequences_are_declared : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        const string eventTypeId = nameof(EventWithStringProperty);
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(eventType);
        _eventTypes.GetSchemaFor((EventTypeId)eventTypeId).Returns(_generator.Generate(typeof(EventWithStringProperty)));

        _constraintBuilder.Unique(b => b.On<EventWithStringProperty>(e => e.SomeProperty));
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_apply_to_every_event_sequence() => ((UniqueConstraintDefinition)_result[0]).EventSequences.ShouldBeEmpty();
}
