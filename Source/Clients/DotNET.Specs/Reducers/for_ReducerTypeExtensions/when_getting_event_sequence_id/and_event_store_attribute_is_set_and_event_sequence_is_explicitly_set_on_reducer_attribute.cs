// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_event_sequence_id;

public class and_event_store_attribute_is_set_and_event_sequence_is_explicitly_set_on_reducer_attribute : Specification
{
    record ReadModel;

    [EventType]
    class EventWithoutEventStore;

    [EventStore("some-event-store")]
    [Reducer(eventSequence: "custom-sequence")]
    class InvalidReducer : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(EventWithoutEventStore @event, ReadModel? current) => new();
    }

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => typeof(InvalidReducer).GetEventSequenceId());

    [Fact] void should_throw_event_store_cannot_be_combined_with_explicit_event_sequence() =>
        _exception.ShouldBeOfExactType<EventStoreCannotBeCombinedWithExplicitEventSequence>();
}
