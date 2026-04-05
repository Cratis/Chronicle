// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_event_types_belong_to_the_same_store_as_the_current_store : Specification
{
    const string CurrentEventStore = "my-store";

    [EventType]
    [EventStore(CurrentEventStore)]
    class EventFromSameStore;

    [Reactor]
    class ReactorWithEventTypeFromSameStore : IReactor
    {
        public Task Handle(EventFromSameStore @event) => Task.CompletedTask;
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReactorWithEventTypeFromSameStore).GetEventSequenceId(CurrentEventStore);

    [Fact] void should_return_the_event_log_sequence() => _result.ShouldEqual(EventSequenceId.Log);
}
