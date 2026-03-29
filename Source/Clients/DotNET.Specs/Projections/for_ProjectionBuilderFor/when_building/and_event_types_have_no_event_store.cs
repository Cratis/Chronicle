// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_event_types_have_no_event_store : given.a_projection_builder
{
    [EventType]
    class EventWithNoEventStore;

    protected override IEnumerable<Type> EventTypes => [typeof(EventWithNoEventStore)];

    ProjectionDefinition _result;

    void Because()
    {
        builder.From<EventWithNoEventStore>();
        _result = builder.Build();
    }

    [Fact] void should_use_the_event_log_sequence() =>
        _result.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
}
