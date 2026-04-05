// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_event_sequence_id;

public class and_event_sequence_attribute_is_set : Specification
{
    const string ExplicitEventSequence = "my-custom-sequence";

    record ReadModel;

    [EventType]
    [EventStore("some-event-store")]
    class EventFromExternalStore;

    [EventSequence(ExplicitEventSequence)]
    [Reducer]
    class ReducerWithEventSequenceAttribute : IReducerFor<ReadModel>
    {
        public ReadModel Reduce(EventFromExternalStore @event, ReadModel? current) => new();
    }

    EventSequenceId _result;

    void Because() => _result = typeof(ReducerWithEventSequenceAttribute).GetEventSequenceId();

    [Fact] void should_return_the_explicitly_set_event_sequence() =>
        _result.ShouldEqual(new EventSequenceId(ExplicitEventSequence));
}
