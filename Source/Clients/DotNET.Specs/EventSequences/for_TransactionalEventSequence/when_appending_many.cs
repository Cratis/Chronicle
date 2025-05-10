// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class when_appending_many : given.a_transactional_event_sequence
{
    EventSourceId _eventSourceId;
    IEnumerable<object> _events;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _events = [new SomeEvent("Test1"), new SomeEvent("Test2")];
        _eventStreamType = Guid.NewGuid().ToString();
        _eventStreamId = Guid.NewGuid().ToString();
        _eventSourceType = Guid.NewGuid().ToString();
    }
    Task Because() => _transactionalEventSequence.AppendMany(
        _eventSourceId,
        _events,
        _eventStreamType,
        _eventStreamId,
        _eventSourceType);

    [Fact]
    void should_add_events_to_unit_of_work() => _unitOfWork.Received(2).AddEvent(
            _eventSequence.Id,
            _eventSourceId,
            Arg.Any<object>(),
            Arg.Any<Causation>(),
            _eventStreamType,
            _eventStreamId,
            _eventSourceType);
    [Fact]
    void should_add_first_event_to_unit_of_work_with_correct_event() => _unitOfWork.Received(1).AddEvent(
            _eventSequence.Id,
            _eventSourceId,
            _events.ElementAt(0),
            Arg.Any<Causation>(),
            _eventStreamType,
            _eventStreamId,
            _eventSourceType);

    [Fact]
    void should_add_second_event_to_unit_of_work_with_correct_event() => _unitOfWork.Received(1).AddEvent(
            _eventSequence.Id,
            _eventSourceId,
            _events.ElementAt(1),
            Arg.Any<Causation>(),
            _eventStreamType,
            _eventStreamId,
            _eventSourceType);
}
