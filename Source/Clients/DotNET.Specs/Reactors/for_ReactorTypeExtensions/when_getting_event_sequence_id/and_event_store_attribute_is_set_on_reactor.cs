// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_event_store_attribute_is_set_on_reactor : Specification
{
    const string SourceEventStore = "some-event-store";

    [EventType]
    class EventWithoutEventStore;

    [EventStore(SourceEventStore)]
    [Reactor]
    class ReactorWithEventStoreAttribute : IReactor
    {
        public Task Handle(EventWithoutEventStore @event) => Task.CompletedTask;
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReactorWithEventStoreAttribute).GetEventSequenceId();

    [Fact] void should_return_the_inbox_event_sequence_for_the_source_event_store() =>
        _result.ShouldEqual(new EventSequenceId($"{EventSequenceId.InboxPrefix}{SourceEventStore}"));
}
