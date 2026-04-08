// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_event_sequence_id;

public class and_event_sequence_is_explicitly_set : Specification
{
    const string ExplicitEventSequence = "my-custom-sequence";

    [EventType]
    [EventStore("some-event-store")]
    class EventFromExternalStore;

    [Reactor(eventSequence: ExplicitEventSequence)]
    class ReactorWithExplicitEventSequence : IReactor
    {
        public Task Handle(EventFromExternalStore @event) => Task.CompletedTask;
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReactorWithExplicitEventSequence).GetEventSequenceId();

    [Fact] void should_return_the_explicitly_set_event_sequence() =>
        _result.ShouldEqual(new EventSequenceId(ExplicitEventSequence));
}
