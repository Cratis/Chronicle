// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes.given;

/// <summary>
/// A real <see cref="ReducerObserverSubscriber"/> for one event store and namespace, whose notifier lookups are
/// routed through a <see cref="NotifierRouter"/> and whose pipeline produces one change per delivery.
/// </summary>
public class ReducerProducer
{
    ReducerObserverSubscriber _subscriber = null!;
    ObserverSubscriberKey _key = null!;
    string _value = string.Empty;
    ulong _sequence;

    public static async Task<ReducerProducer> Create(NotifierRouter router, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var producer = new ReducerProducer
        {
            _key = new ObserverSubscriberKey(observerId, eventStore, @namespace, EventSequenceId.Log, "the-partition", string.Empty)
        };
        await producer.Initialize(router);
        return producer;
    }

    /// <summary>
    /// Delivers events to the reducer; the resulting change carries the value.
    /// </summary>
    /// <param name="value">The value the resulting change carries.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Produce(string value)
    {
        _value = value;
        var result = await _subscriber.OnNext("source", [], new ObserverSubscriberContext(new ConnectedClient()));
        result.State.ShouldEqual(ObserverSubscriberState.Ok);
    }

    async Task Initialize(NotifierRouter router)
    {
        var silo = new TestKitSilo();
        silo.AddProbe(identity => router.For(identity.ToString()));

        var definition = new ReducerDefinition(_key.ObserverId.Value, EventSequenceId.Log, [], "the-model", true, []);
        var stateStorage = Substitute.For<IStorage<ReducerDefinition>>();
        stateStorage.State = definition;
        silo.Options.StorageFactory = _ => stateStorage;

        var pipeline = Substitute.For<IReducerPipeline>();
        pipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>()).Returns<Task<ReadModelChangeNotification?>>(async call =>
        {
            await call.Arg<ReducerDelegate>()([], null);
            return new ReadModelChangeNotification(
                "source",
                new JsonObject { ["value"] = _value },
                new ReadModelChangeContext(ReadModelChangeType.Added, ++_sequence, DateTimeOffset.UtcNow, CorrelationId.New()));
        });
        var pipelineFactory = Substitute.For<IReducerPipelineFactory>();
        pipelineFactory.Create(_key.EventStore, _key.Namespace, Arg.Any<ReducerDefinition>()).Returns(pipeline);
        silo.AddService(pipelineFactory);

        var mediator = Substitute.For<IReducerMediator>();
        mediator.When(_ => _.OnNext(
                Arg.Any<ReducerId>(),
                Arg.Any<ConnectionId>(),
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<ReduceOperation>(),
                Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(call => call.Arg<TaskCompletionSource<ReducerSubscriberResult>>().SetResult(
                new ReducerSubscriberResult(ObserverSubscriberResult.Ok(42), null)));
        silo.AddService(mediator);
        silo.AddService(TimeProvider.System);
        silo.AddService(Substitute.For<ILogger<ReducerObserverSubscriber>>());

        _subscriber = await silo.CreateGrainAsync<ReducerObserverSubscriber>(_key.ToString());
    }
}
