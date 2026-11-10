// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class two_observation_streams_for_the_same_connection : Specification
{
    const string ConnectionIdentifier = "connection";
    const string StoreName = "store";
    const string NamespaceName = "namespace";
    const string ReducerIdentifier = "reducer";

    protected readonly Subject<ReducerMessage> _earlierMessages = new();
    protected readonly Subject<ReducerMessage> _laterMessages = new();
    protected readonly List<ReduceOperationMessage> _earlierDeliveries = [];
    protected readonly List<ReduceOperationMessage> _laterDeliveries = [];
    protected IReducer _reducer;
    protected ReducerMediator _mediator;
    protected SignallingMediator _signals;
    protected RecordingLogger _logger;
    protected IDisposable _earlierSubscription;
    protected IDisposable? _laterSubscription;
    Reducers _reducers;

    async Task Establish()
    {
        _reducer = Substitute.For<IReducer>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IReducer>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reducer);
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

        _mediator = new ReducerMediator();
        _signals = new SignallingMediator(_mediator);
        _logger = new RecordingLogger();
        _reducers = new Reducers(
            grainFactory,
            _signals,
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reducers>(),
            Options.Create(new ChronicleOptions()),
            AllowingConnectionOwnership.Create(),
            _logger,
            Substitute.For<IObserverRegistrationGate>(),
            NamespaceCasingContext.Create());

        _earlierSubscription = _reducers.Observe(_earlierMessages).Subscribe(_earlierDeliveries.Add);
        _earlierMessages.OnNext(Register());
        await _signals.Subscribed(0).WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected async Task OpenLaterStream()
    {
        _laterSubscription = _reducers.Observe(_laterMessages).Subscribe(_laterDeliveries.Add);
        _laterMessages.OnNext(Register());
        await _signals.Subscribed(1).WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected void Deliver(TaskCompletionSource<ReducerSubscriberResult> result) =>
        _mediator.OnNext(ReducerIdentifier, ConnectionIdentifier, StoreName, NamespaceName, new ReduceOperation("partition", [], null), result);

    static ReducerMessage Register() => new(new(new RegisterReducer
    {
        ConnectionId = ConnectionIdentifier,
        EventStore = StoreName,
        Namespace = NamespaceName,
        Reducer = new Contracts.Observation.Reducers.ReducerDefinition { ReducerId = ReducerIdentifier, EventSequenceId = "event-log", ReadModel = "model" }
    }));

    void Destroy()
    {
        _earlierSubscription.Dispose();
        _laterSubscription?.Dispose();
        _earlierMessages.Dispose();
        _laterMessages.Dispose();
    }

    /// <summary>
    /// Forwards to a real mediator and signals once a stream has subscribed or has finished disconnecting.
    /// </summary>
    /// <param name="inner">The real <see cref="ReducerMediator"/> to forward to.</param>
    protected sealed class SignallingMediator(ReducerMediator inner) : IReducerMediator
    {
        readonly TaskCompletionSource[] _subscribed = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        readonly TaskCompletionSource[] _disconnected = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        int _subscribeCount;
        int _disconnectCount;

        public Task Subscribed(int index) => _subscribed[index].Task;

        public Task Disconnected(int index) => _disconnected[index].Task;

        public void Subscribe(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerEventsObserver target)
        {
            inner.Subscribe(reducerId, connectionId, eventStore, @namespace, target);
            _subscribed[Interlocked.Increment(ref _subscribeCount) - 1].TrySetResult();
        }

        public void SubscribePages(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerPageObserver target, Concepts.Observation.PageDeliveryDefinition? pageDelivery = null) =>
            inner.SubscribePages(reducerId, connectionId, eventStore, @namespace, target, pageDelivery);

        public Concepts.Observation.PageDeliveryDefinition? GetPageDelivery(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
            inner.GetPageDelivery(reducerId, connectionId, eventStore, @namespace);

        public void OnNextPage(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, IReadOnlyList<ReduceOperation> operations, IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> completions) =>
            inner.OnNextPage(reducerId, connectionId, eventStore, @namespace, operations, completions);

        public void SubscribeReplayNotifications(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerReplayObserver target) =>
            inner.SubscribeReplayNotifications(reducerId, connectionId, eventStore, @namespace, target);

        public void OnNext(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReduceOperation operation, TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource) =>
            inner.OnNext(reducerId, connectionId, eventStore, @namespace, operation, taskCompletionSource);

        public void OnBeginReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
            inner.OnBeginReplay(reducerId, eventStore, @namespace);

        public void OnEndReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
            inner.OnEndReplay(reducerId, eventStore, @namespace);

        public void OnBeginReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
            inner.OnBeginReplayPartition(reducerId, eventStore, @namespace, partition);

        public void OnEndReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
            inner.OnEndReplayPartition(reducerId, eventStore, @namespace, partition);

        public bool Disconnected(ReducerId reducerId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerEventsObserver target, ReducerReplayObserver replayTarget)
        {
            var ownsRegistration = inner.Disconnected(reducerId, connectionId, eventStore, @namespace, target, replayTarget);
            _disconnected[Interlocked.Increment(ref _disconnectCount) - 1].TrySetResult();
            return ownsRegistration;
        }
    }

    /// <summary>
    /// Records the event ids that were logged so a spec can await one without sleeping.
    /// </summary>
    protected sealed class RecordingLogger : ILogger<Reducers>
    {
        readonly TaskCompletionSource<EventId> _replaced = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<EventId> StreamReplaced => _replaced.Task;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == "StreamReplacedKeepingSubscription")
            {
                _replaced.TrySetResult(eventId);
            }
        }
    }
}
