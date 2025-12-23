// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_snapshots_by_id;

public class and_it_is_a_reducer : given.all_dependencies
{
    public class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    ReducerSnapshot<MyReadModel> _reducerSnapshot;
    IEnumerable<ReadModelSnapshot<MyReadModel>> _result;

    void Establish()
    {
        _key = "test-key";

        var events = new[]
        {
            CreateAppendedEvent(new EventSequenceNumber(1)),
            CreateAppendedEvent(new EventSequenceNumber(2))
        };

        _reducerSnapshot = new ReducerSnapshot<MyReadModel>(
            new MyReadModel { Name = "Test" },
            events,
            DateTimeOffset.UtcNow,
            CorrelationId.New());

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasReducerFor(typeof(MyReadModel)).Returns(true);
        _reducers.GetSnapshotsById<MyReadModel>(_key).Returns([_reducerSnapshot]);
    }

    async Task Because() => _result = await _readModels.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor(typeof(MyReadModel));
    [Fact] void should_check_if_reducer_exists() => _reducers.Received(1).HasReducerFor(typeof(MyReadModel));
    [Fact] void should_get_snapshots_from_reducer() => _reducers.Received(1).GetSnapshotsById<MyReadModel>(_key);
    [Fact] void should_return_one_snapshot() => _result.Count().ShouldEqual(1);
    [Fact] void should_return_snapshot_with_correct_instance() => _result.First().Instance.Name.ShouldEqual("Test");
    [Fact] void should_return_snapshot_with_correct_correlation_id() => _result.First().CorrelationId.ShouldEqual(_reducerSnapshot.CorrelationId);
    [Fact] void should_return_snapshot_with_events() => _result.First().Events.Count().ShouldEqual(2);

    static AppendedEvent CreateAppendedEvent(EventSequenceNumber sequenceNumber)
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
            CorrelationId.New(),
            DateTimeOffset.UtcNow);

        return new AppendedEvent(context, new { });
    }
}
