// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_event_sequence_id;

public class and_no_event_sequence_is_set_and_all_event_types_have_the_same_event_store : Specification
{
    const string SourceEventStore = "some-event-store";

    record ReadModel;

    [EventType]
    [EventStore(SourceEventStore)]
    class EventFromExternalStore;

    [Reducer]
    class ReducerWithEventTypeFromExternalStore : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(EventFromExternalStore @event, ReadModel? current) => new();
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReducerWithEventTypeFromExternalStore).GetEventSequenceId();

    [Fact] void should_return_the_inbox_event_sequence_for_the_source_event_store() =>
        _result.ShouldEqual(new EventSequenceId($"{EventSequenceId.InboxPrefix}{SourceEventStore}"));
}
