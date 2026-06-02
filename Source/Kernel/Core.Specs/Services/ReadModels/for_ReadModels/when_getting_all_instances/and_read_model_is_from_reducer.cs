// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_read_model_is_from_reducer : given.all_dependencies
{
    GetAllInstancesResponse _result = null!;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            ObserverType = Concepts.ReadModels.ReadModelObserverType.Reducer,
            ObserverIdentifier = "my-reducer"
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        var observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(observer);

        var reducerEventType = new EventType("my-event", 1);
        observer.GetSubscription().Returns(new ObserverSubscription(
            "my-reducer",
            new ObserverKey("my-reducer", "test-store", "test-namespace", "event-log"),
            [reducerEventType],
            typeof(IReducerObserverSubscriber),
            SiloAddress.Zero,
            new ConnectedClient { ConnectionId = "connection-id" }));
        observer.GetEventTypes().Returns([reducerEventType]);

        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([
            new AppendedEvent(EventContext.EmptyWithEventSourceId("source-1") with { SequenceNumber = 1 }, new ExpandoObject()),
            new AppendedEvent(EventContext.EmptyWithEventSourceId("source-2") with { SequenceNumber = 2 }, new ExpandoObject())
        ]);
        eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        _namespaceStorage.GetEventSequence("event-log").Returns(eventSequenceStorage);

        var readModelDefinitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _eventStoreStorage.ReadModels.Returns(readModelDefinitionsStorage);
        readModelDefinitionsStorage.Get(_readModelDefinition.Identifier).Returns(_readModelDefinition);

        _reducerMediator
            .When(_ => _.OnNext(
                "my-reducer",
                "connection-id",
                "test-store",
                "test-namespace",
                Arg.Any<ReduceOperation>(),
                Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(call =>
            {
                var operation = call.Arg<ReduceOperation>();
                var state = new ExpandoObject();
                ((IDictionary<string, object?>)state)["name"] = operation.Partition.ToString();
                var tcs = call.Arg<TaskCompletionSource<ReducerSubscriberResult>>();
                tcs.SetResult(new(ObserverSubscriberResult.Ok(2), state));
            });

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<Schemas.JsonSchema>())
            .Returns(call =>
            {
                var jsonObject = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    jsonObject[key] = value?.ToString();
                }

                return jsonObject;
            });
    }

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = ulong.MaxValue
    });

    [Fact] void should_return_two_instances() => _result.Instances.Count.ShouldEqual(2);
    [Fact] void should_return_processed_event_count() => _result.ProcessedEventsCount.ShouldEqual(2UL);
    [Fact] void should_include_first_partition_result() => _result.Instances.Any(json => JsonSerializer.Deserialize<JsonElement>(json).GetProperty("name").GetString() == "source-1").ShouldBeTrue();
    [Fact] void should_include_second_partition_result() => _result.Instances.Any(json => JsonSerializer.Deserialize<JsonElement>(json).GetProperty("name").GetString() == "source-2").ShouldBeTrue();
}
