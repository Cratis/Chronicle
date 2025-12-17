// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_snapshots_by_id;

public class and_reducer_returns_null : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    IReducerHandler _handler;
    IEventSequence _eventSequence;
    ImmutableList<AppendedEvent> _events;
    IEnumerable<ReducerSnapshot<MyReadModel>> _result;
    CorrelationId _correlationId;

    void Establish()
    {
        _key = "test-key";
        _correlationId = CorrelationId.New();

        _handler = Substitute.For<IReducerHandler>();
        _handler.ReadModelType.Returns(typeof(MyReadModel));
        _handler.EventSequenceId.Returns(new EventSequenceId("test-sequence"));
        _handler.EventTypes.Returns([new EventType("test-event", 1)]);

        _handlersByModelType[typeof(MyReadModel)] = _handler;

        _eventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);

        var event1 = CreateAppendedEvent(new EventSequenceNumber(1), _correlationId);
        _events = [event1];
        _eventSequence.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>()).Returns(_events);

        _handler.OnNext(
            Arg.Any<IEnumerable<AppendedEvent>>(),
            Arg.Any<object?>(),
            Arg.Any<IServiceProvider>()).Returns(new ReduceResult(
                null,
                new EventSequenceNumber(1),
                [],
                string.Empty));
    }

    async Task Because() => _result = await _reducers.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_not_include_snapshot_when_null() => _result.ShouldBeEmpty();

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
