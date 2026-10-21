// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class an_observation_stream : Specification
{
    protected readonly Subject<ReducerMessage> _messages = new();
    protected readonly List<ReduceOperationMessage> _deliveries = [];
    protected ReducerEventsObserver _deliver;
    IDisposable _subscription;

    async Task Establish()
    {
        var subscribed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mediator = Substitute.For<IReducerMediator>();
        mediator.When(_ => _.Subscribe(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReducerEventsObserver>()))
            .Do(call =>
            {
                _deliver = call.Arg<ReducerEventsObserver>();
                subscribed.TrySetResult();
            });
        var grainFactory = Substitute.For<IGrainFactory>();
        var readModel = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(readModel);
        var schema = new JsonSchema();
        schema.Properties.Add("id", new JsonSchemaProperty { Type = JsonObjectType.String });
        readModel.GetDefinition().Returns(new ReadModelDefinition(
            "model",
            "models",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            Concepts.Sinks.SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []));
        var reducers = new Reducers(
            grainFactory,
            mediator,
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reducers>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reducers>.Instance);
        _subscription = reducers.Observe(_messages).Subscribe(_deliveries.Add);
        _messages.OnNext(new(new(new RegisterReducer
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reducer = new Contracts.Observation.Reducers.ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = "model" }
        })));
        await subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected void Reply(ulong deliveryId = 0) => _messages.OnNext(new(new(new ReducerResult
    {
        Partition = "partition",
        DeliveryId = deliveryId,
        State = ObservationState.Success,
        LastSuccessfulObservation = 12,
        ExceptionStackTrace = string.Empty
    })));

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }
}
