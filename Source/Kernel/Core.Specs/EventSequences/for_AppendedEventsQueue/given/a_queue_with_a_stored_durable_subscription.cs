// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A queue of an event store whose storage holds the durable subscription of one observer that has no activation.
/// </summary>
public class a_queue_with_a_stored_durable_subscription : all_dependencies
{
    protected const string EventStore = "store";
    protected const string Namespace = "namespace";
    protected static readonly EventSequenceId _eventSequence = EventSequenceId.Log;
    protected static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    protected readonly DeliveryClock _clock = new();
    protected readonly ConcurrentQueue<string> _calls = new();
    protected AppendedEventsQueue _queue;
    protected IObserver _observer;
    protected ObserverKey _observerKey;
    protected IClusterReleases _clusterReleases;
    protected IWakeAdmission _wakeAdmission;
    protected IStorage _storage;
    protected IObserverStateStorage _observerStates;
    protected IObserverDefinitionsStorage _observerDefinitions;
    protected int _handledEvents;

    protected virtual ObserverSubscriptionKind StoredKind => ObserverSubscriptionKind.ClientReactor;

    protected virtual bool FloorIsReached => true;

    protected virtual DurableSubscriptionMode Mode => DurableSubscriptionMode.Release;

    protected virtual int DormantAfter => 900;

    protected virtual int SafetyNetSeconds => 900;

    internal virtual AppendedEventsQueueMetrics? CreateMetrics() => null;

    protected virtual IWakeAdmission CreateWakeAdmission() => new WakeAdmission(0, Environment.ProcessorCount);

    protected static AppendedEvent EventAt(ulong sequenceNumber, string partition = "partition") =>
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, sequenceNumber) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, sequenceNumber).Context with
            {
                EventSourceId = partition
            }
        };

    protected ObserverKey KeyOf(ObserverId id) => new(id, EventStore, Namespace, _eventSequence);

    protected ObserverId IdInQueue(int queue, string name)
    {
        var router = new AppendedEventsQueueRouter(new ChronicleOptions().Events.Queues);
        return Enumerable.Range(0, 1000).Select(index => $"{name}-{index}").First(id => router.GetQueueIndexFor(KeyOf(id)) == queue);
    }

    protected AppendedEventsQueue CreateQueue(IWakeAdmission? wakeAdmission = null, ObserverId[]? alsoStored = null)
    {
        var options = new ChronicleOptions
        {
            Observers = new() { DormantAfter = DormantAfter, DormantSafetyNet = SafetyNetSeconds, DurableSubscriptions = Mode }
        };
        var queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(options),
            Substitute.For<ILogger<AppendedEventsQueue>>(),
            storage: _storage,
            clusterReleases: _clusterReleases,
            wakeAdmission: wakeAdmission ?? _wakeAdmission)
        {
            CatchupTimeProvider = _clock
        };
        queue.Identify(new EventSequenceKey(_eventSequence, EventStore, Namespace), 0);
        queue.Metrics = CreateMetrics();
        return queue;
    }

    protected void Store(ObserverId id, ObserverSubscriptionKind kind)
    {
        var stored = new Dictionary<ObserverId, ObserverDurableSubscription>(_observerStates.GetDurableSubscriptions(_eventSequence).Result)
        {
            [id] = new(kind, false, null, [new("a-connection", "127.0.0.1:11111@1")])
        };
        _observerStates.GetDurableSubscriptions(_eventSequence).Returns(stored);
        _observerDefinitions.Has(id).Returns(true);
        _observerDefinitions.Get(id).Returns(new ObserverDefinition { Identifier = id, EventTypes = [_eventType] });
    }

    protected IObserver ObserverFor(ObserverKey key, Func<Task>? onEnsure = null)
    {
        var observer = Substitute.For<IObserver>();
        observer.Ensure().Returns(async _ =>
        {
            _calls.Enqueue("ensure");
            if (onEnsure is not null) await onEnsure();
        });
        observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(callInfo =>
        {
            _calls.Enqueue("handle");
            Interlocked.Add(ref _handledEvents, callInfo.Arg<IEnumerable<AppendedEvent>>().Count());
            return Task.CompletedTask;
        });
        observer.RunWatchdogOnce().Returns(_ =>
        {
            _calls.Enqueue("watchdog");
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(key).Returns(observer);
        return observer;
    }

    async Task Establish()
    {
        _clusterReleases = Substitute.For<IClusterReleases>();
        _clusterReleases.Summarize().Returns(FloorIsReached
            ? new ClusterReleaseSummary([(new SemanticVersion(19, 173, 0), 1)], 0, 0)
            : new ClusterReleaseSummary([(new SemanticVersion(19, 172, 0), 1), (new SemanticVersion(19, 173, 0), 1)], 0, 0));
        _wakeAdmission = CreateWakeAdmission();

        _storage = Substitute.For<IStorage>();
        _observerStates = Substitute.For<IObserverStateStorage>();
        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        _observerStates.GetDurableSubscriptions(_eventSequence).Returns(new Dictionary<ObserverId, ObserverDurableSubscription>());
        var eventStore = _storage.GetEventStore(EventStore);
        eventStore.GetNamespace(Namespace).Observers.Returns(_observerStates);
        eventStore.Observers.Returns(_observerDefinitions);

        var id = IdInQueue(0, "dormant");
        _observerKey = KeyOf(id);
        Store(id, StoredKind);
        _observer = ObserverFor(_observerKey);
        _queue = CreateQueue();
        await _queue.SeedDormantSubscriptions();
    }
}
