// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModelExplorer;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_snapshots_by_id;

public class and_it_is_a_reducer : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    class MyEvent
    {
        public string Data { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    ReadModelKey _key;
    IEnumerable<ReadModelSnapshot<MyReadModel>> _result;
    QueryResult<IEnumerable<ReadModelSnapshotResponse>> _response;
    IReducerHandler _handler;

    void Establish()
    {
        _key = "test-key";

        _projections.HasFor<MyReadModel>().Returns(false);
        _reducers.HasReducerFor(typeof(MyReadModel)).Returns(true);

        _handler = Substitute.For<IReducerHandler>();
        _handler.EventSequenceId.Returns(new EventSequenceId("custom-sequence"));
        _reducers.GetHandlerForReadModelType(typeof(MyReadModel)).Returns(_handler);

        var eventTypeId = new EventTypeId("my-event");
        var eventType = new EventType(eventTypeId, 1);
        _eventTypes.HasFor(eventTypeId).Returns(true);
        _eventTypes.GetClrTypeFor(eventTypeId).Returns(typeof(MyEvent));

        _response = given.a_snapshot_response.AsResult(
            given.a_snapshot_response.With("""{"Name":"ReducerModel","Value":99}""", eventType, new MyEvent { Data = "reducer-data", Number = 456 }, _jsonSerializerOptions));

        _services.ReadModelExplorer.AllSnapshotsForReadModel(Arg.Any<AllSnapshotsForReadModelRequest>()).Returns(_response);
    }

    async Task Because() => _result = await _readModels.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_return_snapshot() => _result.Count().ShouldEqual(1);
    [Fact] void should_have_deserialized_read_model() => _result.First().Instance.Name.ShouldEqual("ReducerModel");
    [Fact] void should_have_events_in_snapshot() => _result.First().Events.Count().ShouldEqual(1);
    [Fact] void should_have_deserialized_event_to_correct_type() => _result.First().Events.First().Content.ShouldBeOfExactType<MyEvent>();
    [Fact] void should_preserve_event_data() => (_result.First().Events.First().Content as MyEvent).Data.ShouldEqual("reducer-data");
    [Fact] void should_preserve_event_number() => (_result.First().Events.First().Content as MyEvent).Number.ShouldEqual(456);
    [Fact] void should_use_handler_event_sequence_id() => _services.ReadModelExplorer.Received(1).AllSnapshotsForReadModel(Arg.Is<AllSnapshotsForReadModelRequest>(r => r.EventSequenceId == _handler.EventSequenceId));
}
