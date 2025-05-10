// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class when_appending : given.a_transactional_event_sequence
{
    EventSourceId _eventSourceId;
    object _event;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _event = new SomeEvent("Test");
        _eventStreamType = Guid.NewGuid().ToString();
        _eventStreamId = Guid.NewGuid().ToString();
        _eventSourceType = Guid.NewGuid().ToString();
    }

    Task Because() => _transactionalEventSequence.Append(
        _eventSourceId,
        _event,
        _eventStreamType,
        _eventStreamId,
        _eventSourceType);

    [Fact]
    void should_add_event_to_unit_of_work() => _unitOfWork.Received(1).AddEvent(
            _eventSequence.Id,
            _eventSourceId,
            _event,
            Arg.Any<Causation>(),
            _eventStreamType,
            _eventStreamId,
            _eventSourceType);
}
