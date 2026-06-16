// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_event_has_metadata : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;
    Causation _causation;
    ConcurrencyScope _concurrencyScope;
    DateTimeOffset _occurred;
    Subject _subject;
    IEnumerable<string> _tags;
    SomeEvent _event;
    EventForEventSourceId _appendedEvent;

    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
    };

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventStreamType = Guid.NewGuid().ToString();
        _eventStreamId = Guid.NewGuid().ToString();
        _eventSourceType = Guid.NewGuid().ToString();
        _causation = new Causation(DateTimeOffset.UtcNow, "metadata", new Dictionary<string, string>());
        _concurrencyScope = new ConcurrencyScope(42UL, _eventSourceId);
        _occurred = DateTimeOffset.UtcNow.AddMinutes(-5);
        _subject = "some-subject";
        _tags = ["one", "two"];
        _event = new SomeEvent();

        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            _eventSourceId,
            _event,
            _causation,
            _eventStreamType,
            _eventStreamId,
            _eventSourceType,
            _concurrencyScope,
            _tags,
            _occurred,
            _subject);
    }

    async Task Because()
    {
        await _unitOfWork.Commit();
        _appendedEvent = _eventsAppended.ToArray()[0];
    }

    [Fact] void should_preserve_event_source_id() => _appendedEvent.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_preserve_event() => _appendedEvent.Event.ShouldEqual(_event);
    [Fact] void should_preserve_causation() => _appendedEvent.Causation.ShouldEqual(_causation);
    [Fact] void should_preserve_event_stream_type() => _appendedEvent.EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_preserve_event_stream_id() => _appendedEvent.EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_preserve_event_source_type() => _appendedEvent.EventSourceType.ShouldEqual(_eventSourceType);
    [Fact] void should_preserve_tags() => _appendedEvent.Tags.ShouldContainOnly("one", "two");
    [Fact] void should_preserve_occurred() => _appendedEvent.Occurred.ShouldEqual(_occurred);
    [Fact] void should_preserve_subject() => _appendedEvent.Subject.ShouldEqual(_subject);

    record SomeEvent();
}
