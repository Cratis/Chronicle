// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class with_metadata : given.a_transactional_event_sequence
{
    EventSourceId _eventSourceId;
    SomeEvent _event;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;
    ConcurrencyScope _concurrencyScope;
    DateTimeOffset _occurred;
    Subject _subject;
    IEnumerable<string> _tags;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _event = new SomeEvent("Test");
        _eventStreamType = Guid.NewGuid().ToString();
        _eventStreamId = Guid.NewGuid().ToString();
        _eventSourceType = Guid.NewGuid().ToString();
        _concurrencyScope = new ConcurrencyScope(42UL, _eventSourceId);
        _occurred = DateTimeOffset.UtcNow.AddMinutes(-5);
        _subject = "some-subject";
        _tags = ["one", "two"];
    }

    Task Because() => _transactionalEventSequence.Append(
        _eventSourceId,
        _event,
        _eventStreamType,
        _eventStreamId,
        _eventSourceType,
        _concurrencyScope,
        _tags,
        _occurred,
        _subject);

    [Fact]
    void should_add_event_to_unit_of_work_with_metadata() => _unitOfWork.Received(1).AddEvent(
        _eventSequence.Id,
        _eventSourceId,
        _event,
        Arg.Any<Causation>(),
        _eventStreamType,
        _eventStreamId,
        _eventSourceType,
        _concurrencyScope,
        _tags,
        _occurred,
        _subject);
}
