// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_event_types_have_different_event_stores : given.a_projection_builder
{
    [EventType]
    [EventStore("event-store-one")]
    class EventFromFirstStore;

    [EventType]
    [EventStore("event-store-two")]
    class EventFromSecondStore;

    protected override IEnumerable<Type> EventTypes => [typeof(EventFromFirstStore), typeof(EventFromSecondStore)];

    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
    {
        builder.From<EventFromFirstStore>();
        builder.From<EventFromSecondStore>();
        builder.Build();
    });

    [Fact] void should_throw_multiple_event_stores_defined() => _exception.ShouldBeOfExactType<MultipleEventStoresDefined>();
}
