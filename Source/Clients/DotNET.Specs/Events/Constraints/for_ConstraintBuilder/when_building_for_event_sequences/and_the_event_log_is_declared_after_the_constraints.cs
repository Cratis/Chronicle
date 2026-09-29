// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_for_event_sequences;

/// <summary>
/// The declaration holds for every constraint on the builder, wherever in the chain it is written - so an author
/// who adds it at the end of an existing chain does not silently leave the constraints above it applying everywhere.
/// </summary>
public class and_the_event_log_is_declared_after_the_constraints : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        const string eventTypeId = nameof(EventWithStringProperty);
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(eventType);
        _eventTypes.GetSchemaFor((EventTypeId)eventTypeId).Returns(_generator.Generate(typeof(EventWithStringProperty)));

        var otherEventType = new EventType(nameof(EventWithIntProperty), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithIntProperty)).Returns(otherEventType);

        _constraintBuilder
            .Unique(b => b.On<EventWithStringProperty>(e => e.SomeProperty))
            .Unique<EventWithIntProperty>()
            .ForEventLog();
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_apply_the_unique_constraint_to_the_event_log_only() => ((UniqueConstraintDefinition)_result[0]).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
    [Fact] void should_apply_the_unique_event_type_constraint_to_the_event_log_only() => ((UniqueEventTypeConstraintDefinition)_result[1]).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
}
