// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_event_sequence_is_explicitly_set_via_from_event_sequence : given.a_projection_builder
{
    const string ExplicitEventSequence = "my-custom-sequence";

    [EventType]
    [EventStore("some-event-store")]
    class EventFromExternalStore;

    protected override IEnumerable<Type> EventTypes => [typeof(EventFromExternalStore)];

    ProjectionDefinition _result;

    void Because()
    {
        builder.FromEventSequence(new EventSequenceId(ExplicitEventSequence));
        builder.From<EventFromExternalStore>();
        _result = builder.Build();
    }

    [Fact] void should_use_the_explicitly_set_event_sequence() =>
        _result.EventSequenceId.ShouldEqual(ExplicitEventSequence);
}
