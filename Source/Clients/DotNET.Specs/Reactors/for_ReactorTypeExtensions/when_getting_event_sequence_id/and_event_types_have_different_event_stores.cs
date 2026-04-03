// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_event_types_have_different_event_stores : Specification
{
    [EventType]
    [EventStore("event-store-one")]
    class EventFromFirstStore;

    [EventType]
    [EventStore("event-store-two")]
    class EventFromSecondStore;

    [Reactor]
    class ReactorWithMixedEventStores : IReactor
    {
        public Task Handle(EventFromFirstStore @event) => Task.CompletedTask;

        public Task Handle(EventFromSecondStore @event) => Task.CompletedTask;
    }

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => typeof(ReactorWithMixedEventStores).GetEventSequenceId());

    [Fact] void should_throw_multiple_event_stores_defined() => _exception.ShouldBeOfExactType<MultipleEventStoresDefined>();
}
