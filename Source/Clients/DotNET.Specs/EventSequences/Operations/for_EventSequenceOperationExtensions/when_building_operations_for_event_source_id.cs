// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperationExtensions;

public class when_building_operations_for_event_source_id : Specification
{
    IEventSequence _eventSequence;
    EventSourceId _eventSourceId;
    Action<EventSourceOperations> _configure;
    EventSequenceOperations _result;
    EventSequenceOperations _expectedOperations;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSourceId = Guid.NewGuid().ToString();
        _configure = Substitute.For<Action<EventSourceOperations>>();
        _expectedOperations = Substitute.For<EventSequenceOperations>(_eventSequence);
    }

    void Because() => _result = _eventSequence.ForEventSourceId(_eventSourceId, _configure);

    [Fact] void should_return_event_sequence_operations() => _result.ShouldNotBeNull();
    [Fact] void should_return_operations_for_the_given_event_sequence() => _result.EventSequence.ShouldEqual(_eventSequence);
}
