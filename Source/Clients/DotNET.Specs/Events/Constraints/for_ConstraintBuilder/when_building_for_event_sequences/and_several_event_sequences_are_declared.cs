// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building_for_event_sequences;

public class and_several_event_sequences_are_declared : given.a_constraint_builder_with_owner
{
    static readonly EventSequenceId _custom = "custom-sequence";

    IImmutableList<IConstraintDefinition> _result;

    void Establish()
    {
        const string eventTypeId = nameof(EventWithStringProperty);
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(EventWithStringProperty)).Returns(eventType);
        _eventTypes.GetSchemaFor((EventTypeId)eventTypeId).Returns(_generator.Generate(typeof(EventWithStringProperty)));

        _constraintBuilder
            .ForEventSequences(EventSequenceId.Log, _custom)
            .ForEventLog()
            .Unique(b => b.On<EventWithStringProperty>(e => e.SomeProperty));
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_apply_to_every_declared_event_sequence_once() => ((UniqueConstraintDefinition)_result[0]).EventSequences.ShouldContainOnly([EventSequenceId.Log, _custom]);
}
