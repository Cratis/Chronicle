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

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_read_model_is_from_reducer : given.all_dependencies
{
    GetInstanceByKeyResponse _result = null!;

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
            new AppendedEvent(EventContext.EmptyWithEventSourceId("read-model-key") with { SequenceNumber = 42 }, new ExpandoObject())
        ]);
        eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, "read-model-key", eventTypes: Arg.Any<IEnumerable<EventType>>())
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
                var state = new ExpandoObject();
                ((IDictionary<string, object?>)state)["name"] = "FromReducer";
                var tcs = call.Arg<TaskCompletionSource<ReducerSubscriberResult>>();
                tcs.SetResult(new(ObserverSubscriberResult.Ok(42), state));
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

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = "read-model-key"
    });

    [Fact] void should_return_reduced_read_model_json() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).GetProperty("name").GetString().ShouldEqual("FromReducer");
    [Fact] void should_return_processed_event_count() => _result.ProjectedEventsCount.ShouldEqual(1UL);
    [Fact] void should_return_last_handled_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(42UL);
}
