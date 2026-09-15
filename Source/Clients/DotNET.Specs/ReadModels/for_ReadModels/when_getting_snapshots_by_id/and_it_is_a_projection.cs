// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModelExplorer;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_snapshots_by_id;

public class and_it_is_a_projection : given.all_dependencies
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

    void Establish()
    {
        _key = "test-key";

        _projections.HasFor<MyReadModel>().Returns(true);

        var eventTypeId = new EventTypeId("my-event");
        var eventType = new EventType(eventTypeId, 1);
        _eventTypes.HasFor(eventTypeId).Returns(true);
        _eventTypes.GetClrTypeFor(eventTypeId).Returns(typeof(MyEvent));

        _response = given.a_snapshot_response.AsResult(
            given.a_snapshot_response.With("""{"Name":"First","Value":1}""", eventType, new MyEvent { Data = "test-data", Number = 123 }, _jsonSerializerOptions),
            given.a_snapshot_response.With("""{"Name":"Second","Value":2}""", eventType, new MyEvent { Data = "test-data", Number = 123 }, _jsonSerializerOptions));

        _services.ReadModelExplorer.AllSnapshotsForReadModel(Arg.Any<AllSnapshotsForReadModelRequest>()).Returns(_response);
    }

    async Task Because() => _result = await _readModels.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_return_correct_number_of_snapshots() => _result.Count().ShouldEqual(2);
    [Fact] void should_have_deserialized_first_read_model() => _result.First().Instance.Name.ShouldEqual("First");
    [Fact] void should_have_deserialized_second_read_model() => _result.Skip(1).First().Instance.Name.ShouldEqual("Second");
    [Fact] void should_have_events_in_first_snapshot() => _result.First().Events.Count().ShouldEqual(1);
    [Fact] void should_have_events_in_second_snapshot() => _result.Skip(1).First().Events.Count().ShouldEqual(1);
    [Fact] void should_have_deserialized_event_to_correct_type() => _result.First().Events.First().Content.ShouldBeOfExactType<MyEvent>();
    [Fact] void should_preserve_event_data() => (_result.First().Events.First().Content as MyEvent).Data.ShouldEqual("test-data");
    [Fact] void should_preserve_event_number() => (_result.First().Events.First().Content as MyEvent).Number.ShouldEqual(123);
}
