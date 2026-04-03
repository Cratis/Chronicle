// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_all_from_event_types_have_the_same_event_store : given.a_projection_builder
{
    const string SourceEventStore = "some-event-store";

    [EventType]
    [EventStore(SourceEventStore)]
    class EventFromExternalStore;

    protected override IEnumerable<Type> EventTypes => [typeof(EventFromExternalStore)];

    ProjectionDefinition _result;

    void Because()
    {
        builder.From<EventFromExternalStore>();
        _result = builder.Build();
    }

    [Fact] void should_use_the_inbox_event_sequence_for_the_source_event_store() =>
        _result.EventSequenceId.ShouldEqual(new EventSequenceId($"{EventSequenceId.InboxPrefix}{SourceEventStore}").Value);
}
