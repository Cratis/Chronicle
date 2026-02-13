// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
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
    GetSnapshotsByKeyResponse _response;

    void Establish()
    {
        _key = "test-key";

        _projections.HasFor<MyReadModel>().Returns(true);

        var eventTypeId = new EventTypeId("my-event");
        var eventType = new EventType(eventTypeId, 1);
        _eventTypes.GetClrTypeFor(eventTypeId).Returns(typeof(MyEvent));

        var eventContext = EventContext.EmptyWithEventSourceId(Guid.NewGuid()) with
        {
            SequenceNumber = 42,
            EventType = eventType
        };

        var contractEvent = new AppendedEvent(eventContext, new MyEvent { Data = "test-data", Number = 123 })
            .ToContract(_jsonSerializerOptions);

        _response = new GetSnapshotsByKeyResponse
        {
            Snapshots =
            [
                new()
                {
                    ReadModel = """{"Name":"First","Value":1}""",
                    Events = [contractEvent],
                    Occurred = DateTimeOffset.UtcNow,
                    CorrelationId = Guid.NewGuid()
                },
                new()
                {
                    ReadModel = """{"Name":"Second","Value":2}""",
                    Events = [contractEvent],
                    Occurred = DateTimeOffset.UtcNow,
                    CorrelationId = Guid.NewGuid()
                }
            ]
        };

        _services.ReadModels.GetSnapshotsByKey(Arg.Any<GetSnapshotsByKeyRequest>()).Returns(_response);
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
