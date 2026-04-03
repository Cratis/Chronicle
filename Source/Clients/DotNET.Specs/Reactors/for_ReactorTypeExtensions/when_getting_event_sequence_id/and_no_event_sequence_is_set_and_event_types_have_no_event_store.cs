// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_no_event_sequence_is_set_and_event_types_have_no_event_store : Specification
{
    [EventType]
    class EventWithNoEventStore;

    [Reactor]
    class ReactorWithEventTypeWithNoEventStore : IReactor
    {
        public Task Handle(EventWithNoEventStore @event) => Task.CompletedTask;
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReactorWithEventTypeWithNoEventStore).GetEventSequenceId();

    [Fact] void should_return_the_event_log() => _result.ShouldEqual(EventSequenceId.Log);
}
