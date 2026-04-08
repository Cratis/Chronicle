// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_event_sequence_id;

public class and_event_sequence_is_explicitly_set : Specification
{
    const string ExplicitEventSequence = "my-custom-sequence";

    record ReadModel;

    [EventType]
    [EventStore("some-event-store")]
    class EventFromExternalStore;

    [Reducer(eventSequence: ExplicitEventSequence)]
    class ReducerWithExplicitEventSequence : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(EventFromExternalStore @event, ReadModel? current) => new();
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReducerWithExplicitEventSequence).GetEventSequenceId();

    [Fact] void should_return_the_explicitly_set_event_sequence() =>
        _result.ShouldEqual(new EventSequenceId(ExplicitEventSequence));
}
