// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_snapshots_by_id;

public class with_multiple_correlation_ids : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    ReadModelKey _key;
    IReducerHandler _handler;
    IEventSequence _eventSequence;
    ImmutableList<AppendedEvent> _events;
    IEnumerable<ReducerSnapshot<MyReadModel>> _result;
    CorrelationId _correlationId1;
    CorrelationId _correlationId2;

    void Establish()
    {
        _key = "test-key";
        _correlationId1 = CorrelationId.New();
        _correlationId2 = CorrelationId.New();

        _handler = Substitute.For<IReducerHandler>();
        _handler.ReadModelType.Returns(typeof(MyReadModel));
        _handler.EventSequenceId.Returns(new EventSequenceId("test-sequence"));
        _handler.EventTypes.Returns([new EventType("test-event", 1)]);

        _handlersByModelType[typeof(MyReadModel)] = _handler;

        _eventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);

        var event1 = CreateAppendedEvent(new EventSequenceNumber(1), _correlationId1);
        var event2 = CreateAppendedEvent(new EventSequenceNumber(2), _correlationId1);
        var event3 = CreateAppendedEvent(new EventSequenceNumber(3), _correlationId2);

        _events = [event1, event2, event3];
        _eventSequence.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>()).Returns(_events);

        _handler.OnNext(
            Arg.Is<IEnumerable<AppendedEvent>>(events => events.All(_ => _.Context.CorrelationId == _correlationId1)),
            Arg.Any<object?>(),
            Arg.Any<IServiceProvider>()).Returns(new ReduceResult(
                new MyReadModel { Name = "First", Count = 2 },
                new EventSequenceNumber(2),
                [],
                string.Empty));

        _handler.OnNext(
            Arg.Is<IEnumerable<AppendedEvent>>(events => events.All(_ => _.Context.CorrelationId == _correlationId2)),
            Arg.Any<object?>(),
            Arg.Any<IServiceProvider>()).Returns(new ReduceResult(
                new MyReadModel { Name = "Second", Count = 1 },
                new EventSequenceNumber(3),
                [],
                string.Empty));
    }

    async Task Because() => _result = await _reducers.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_get_event_sequence() => _eventStore.Received(1).GetEventSequence(_handler.EventSequenceId);
    [Fact] void should_get_events_for_event_source() => _eventSequence.Received(1).GetForEventSourceIdAndEventTypes(_key, _handler.EventTypes);
    [Fact] void should_return_two_snapshots() => _result.Count().ShouldEqual(2);
    [Fact] void should_have_first_snapshot_with_correct_instance() => _result.First().Instance.Name.ShouldEqual("First");
    [Fact] void should_have_first_snapshot_with_correct_count() => _result.First().Instance.Count.ShouldEqual(2);
    [Fact] void should_have_first_snapshot_with_correct_correlation_id() => _result.First().CorrelationId.ShouldEqual(_correlationId1);
    [Fact] void should_have_second_snapshot_with_correct_instance() => _result.Last().Instance.Name.ShouldEqual("Second");
    [Fact] void should_have_second_snapshot_with_correct_count() => _result.Last().Instance.Count.ShouldEqual(1);
    [Fact] void should_have_second_snapshot_with_correct_correlation_id() => _result.Last().CorrelationId.ShouldEqual(_correlationId2);

    static AppendedEvent CreateAppendedEvent(EventSequenceNumber sequenceNumber, CorrelationId correlationId)
    {
        var context = EventContext.From(
            (EventStoreName)"test-store",
            (EventStoreNamespaceName)"test-namespace",
            new EventType("test-event", 1),
            EventSourceType.Default,
            EventSourceId.New(),
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            correlationId,
            DateTimeOffset.UtcNow);

        return new AppendedEvent(context, new { });
    }
}
