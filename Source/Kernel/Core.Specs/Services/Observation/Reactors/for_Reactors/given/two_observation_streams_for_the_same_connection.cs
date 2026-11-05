// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class two_observation_streams_for_the_same_connection : Specification
{
    const string ConnectionIdentifier = "connection";
    const string StoreName = "store";
    const string NamespaceName = "namespace";
    const string ReactorIdentifier = "reactor";

    protected readonly Subject<ReactorMessage> _earlierMessages = new();
    protected readonly Subject<ReactorMessage> _laterMessages = new();
    protected readonly List<EventsToObserve> _earlierDeliveries = [];
    protected readonly List<EventsToObserve> _laterDeliveries = [];
    protected readonly TaskCompletionSource _unsubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected IReactor _reactor;
    protected ReactorMediator _mediator;
    protected SignallingMediator _signals;
    protected RecordingLogger _logger;
    protected IDisposable _earlierSubscription;
    protected IDisposable? _laterSubscription;
    Reactors _reactors;

    async Task Establish()
    {
        _reactor = Substitute.For<IReactor>();
        _reactor.When(_ => _.Unsubscribe()).Do(_ => _unsubscribed.TrySetResult());
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IReactor>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reactor);

        _mediator = new ReactorMediator();
        _signals = new SignallingMediator(_mediator);
        _logger = new RecordingLogger();
        _reactors = new Reactors(
            grainFactory,
            _signals,
            Substitute.For<IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            AllowingConnectionOwnership.Create(),
            _logger,
            Substitute.For<IObserverRegistrationGate>());

        _earlierSubscription = _reactors.Observe(_earlierMessages).Subscribe(_earlierDeliveries.Add);
        _earlierMessages.OnNext(Register());
        await _signals.Subscribed(0).WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected async Task OpenLaterStream()
    {
        _laterSubscription = _reactors.Observe(_laterMessages).Subscribe(_laterDeliveries.Add);
        _laterMessages.OnNext(Register());
        await _signals.Subscribed(1).WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected void Deliver(TaskCompletionSource<ObserverSubscriberResult> result) =>
        _mediator.OnNext(ReactorIdentifier, ConnectionIdentifier, StoreName, NamespaceName, "partition", [], result);

    static ReactorMessage Register() => new(new(new RegisterReactor
    {
        ConnectionId = ConnectionIdentifier,
        EventStore = StoreName,
        Namespace = NamespaceName,
        Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = ReactorIdentifier, EventSequenceId = "event-log" }
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
    /// <param name="inner">The real <see cref="ReactorMediator"/> to forward to.</param>
    protected sealed class SignallingMediator(ReactorMediator inner) : IReactorMediator
    {
        readonly TaskCompletionSource[] _subscribed = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        readonly TaskCompletionSource[] _disconnected = [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        int _subscribeCount;
        int _disconnectCount;

        public Task Subscribed(int index) => _subscribed[index].Task;

        public Task Disconnected(int index) => _disconnected[index].Task;

        public void Subscribe(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorEventsObserver target)
        {
            inner.Subscribe(reactorId, connectionId, eventStore, @namespace, target);
            _subscribed[Interlocked.Increment(ref _subscribeCount) - 1].TrySetResult();
        }

        public void SubscribeReplayNotifications(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorReplayObserver target) =>
            inner.SubscribeReplayNotifications(reactorId, connectionId, eventStore, @namespace, target);

        public void OnNext(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource, TimeSpan? deliverWithin = null, Concepts.Observation.ObservationOrigin origin = Concepts.Observation.ObservationOrigin.Unknown) =>
            inner.OnNext(reactorId, connectionId, eventStore, @namespace, partition, events, taskCompletionSource, deliverWithin, origin);

        public void OnBeginReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
            inner.OnBeginReplay(reactorId, eventStore, @namespace);

        public void OnEndReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
            inner.OnEndReplay(reactorId, eventStore, @namespace);

        public void OnBeginReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
            inner.OnBeginReplayPartition(reactorId, eventStore, @namespace, partition);

        public void OnEndReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
            inner.OnEndReplayPartition(reactorId, eventStore, @namespace, partition);

        public bool Disconnected(ReactorId reactorId, ConnectionId connectionId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReactorEventsObserver target, ReactorReplayObserver replayTarget)
        {
            var ownsRegistration = inner.Disconnected(reactorId, connectionId, eventStore, @namespace, target, replayTarget);
            _disconnected[Interlocked.Increment(ref _disconnectCount) - 1].TrySetResult();
            return ownsRegistration;
        }
    }

    /// <summary>
    /// Records the event ids that were logged so a spec can await one without sleeping.
    /// </summary>
    protected sealed class RecordingLogger : ILogger<Reactors>
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
