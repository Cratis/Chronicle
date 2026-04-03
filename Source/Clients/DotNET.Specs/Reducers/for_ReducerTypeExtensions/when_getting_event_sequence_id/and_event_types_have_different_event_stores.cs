// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_event_sequence_id;

public class and_event_types_have_different_event_stores : Specification
{
    record ReadModel;

    [EventType]
    [EventStore("event-store-one")]
    class EventFromFirstStore;

    [EventType]
    [EventStore("event-store-two")]
    class EventFromSecondStore;

    [Reducer]
    class ReducerWithMixedEventStores : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(EventFromFirstStore @event, ReadModel? current) => new();

        public ReadModel Reduce(EventFromSecondStore @event, ReadModel? current) => new();
    }

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => typeof(ReducerWithMixedEventStores).GetEventSequenceId());

    [Fact] void should_throw_multiple_event_stores_defined() => _exception.ShouldBeOfExactType<MultipleEventStoresDefined>();
}
