// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_event_log_attribute_is_set : Specification
{
    [EventType]
    [EventStore("some-event-store")]
    class EventFromExternalStore;

    [EventLog]
    [Reactor]
    class ReactorWithEventLogAttribute : IReactor
    {
        public Task Handle(EventFromExternalStore @event) => Task.CompletedTask;
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReactorWithEventLogAttribute).GetEventSequenceId();

    [Fact] void should_return_the_event_log_sequence() => _result.ShouldEqual(EventSequenceId.Log);
}
