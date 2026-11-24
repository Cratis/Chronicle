// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A queue whose routing table holds an observer that has an instance in the namespace (a stored cursor) and no live
/// subscription on the queue.
/// </summary>
public class a_queue_with_an_unsubscribed_instance : all_dependencies
{
    protected const string EventStore = "store";
    protected const string Namespace = "namespace";
    protected static readonly EventSequenceId _eventSequence = EventSequenceId.Log;
    protected static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    protected readonly DeliveryClock _clock = new();
    protected readonly ConcurrentQueue<string> _calls = new();
    protected readonly Dictionary<EventTypeId, EventSequenceNumber> _tails = [];
    protected readonly Dictionary<ObserverId, EventSequenceNumber> _cursors = [];
    protected readonly List<ObserverDefinition> _definitions = [];
    protected AppendedEventsQueue _queue;
    protected IObserver _observer;
    protected ObserverKey _observerKey;
    protected IStorage _storage;
    protected IObserverStateStorage _observerStates;
    protected IEventSequence _eventSequenceGrain;
    protected TaskCompletionSource _rung = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected virtual int SafetyNetSeconds => 900;

    internal virtual AppendedEventsQueueMetrics? CreateMetrics() => null;

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
        return Enumerable.Range(0, 1000).Select(index => new ObserverId($"{name}-{index}")).First(id => router.GetQueueIndexFor(id) == queue);
    }

    protected void Route(ObserverId id) =>
        _definitions.Add(new ObserverDefinition(id, EventTypeSet.From([_eventType]), _eventSequence, ObserverType.Reactor, ObserverOwner.Client, true));

    protected AppendedEventsQueue CreateQueue()
    {
        var options = new ChronicleOptions
        {
            Observers = new() { DormantSafetyNet = SafetyNetSeconds }
        };
        var queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Options.Create(options),
            Substitute.For<ILogger<AppendedEventsQueue>>(),
            storage: _storage,
            wakeAdmission: new WakeAdmission(0, Environment.ProcessorCount),
            routing: new TableOf(this))
        {
            CatchupTimeProvider = _clock
        };
        queue.Identify(new EventSequenceKey(_eventSequence, EventStore, Namespace), 0);
        queue.Metrics = CreateMetrics();
        return queue;
    }

    protected IObserver ObserverFor(ObserverKey key)
    {
        var observer = Substitute.For<IObserver>();
        observer.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(_ =>
        {
            _calls.Enqueue($"handle:{key.ObserverId}");
            return Task.CompletedTask;
        });
        observer.RunWatchdogOnce().Returns(_ =>
        {
            _calls.Enqueue($"ring:{key.ObserverId}");
            _rung.TrySetResult();
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(key).Returns(observer);
        return observer;
    }

    protected async Task Deliver(params AppendedEvent[] events)
    {
        await _queue.Enqueue(events);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    protected async Task<bool> WasRung()
    {
        try
        {
            await _rung.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    async Task Establish()
    {
        _storage = Substitute.For<IStorage>();
        _observerStates = Substitute.For<IObserverStateStorage>();
        _observerStates.GetNextEventSequenceNumbers(_eventSequence).Returns(_ =>
            Task.FromResult<IReadOnlyDictionary<ObserverId, EventSequenceNumber>>(new Dictionary<ObserverId, EventSequenceNumber>(_cursors)));
        _eventSequenceGrain = Substitute.For<IEventSequence>();
        _eventSequenceGrain.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(callInfo =>
            Task.FromResult(callInfo.Arg<IEnumerable<EventType>>()
                .Select(eventType => _tails.GetValueOrDefault(eventType.Id, EventSequenceNumber.Unavailable))
                .OrderDescending()
                .First()));
        _grainFactory.GetGrain<IEventSequence>(new EventSequenceKey(_eventSequence, EventStore, Namespace)).Returns(_eventSequenceGrain);
        _storage.GetEventStore(EventStore).GetNamespace(Namespace).Observers.Returns(_observerStates);

        var id = IdInQueue(0, "unsubscribed");
        _observerKey = KeyOf(id);
        Route(id);
        _cursors[id] = 5;
        _observer = ObserverFor(_observerKey);
        _queue = CreateQueue();
        await _queue.ReadInstances();
    }

    void Destroy() => _queue?.Dispose();

    /// <summary>
    /// An <see cref="IObservers"/> that answers with the routes the specification set up.
    /// </summary>
    /// <param name="spec">The specification holding the definitions.</param>
    sealed class TableOf(a_queue_with_an_unsubscribed_instance spec) : IObservers
    {
        public ValueTask<ObserverRoutingTable> GetTable(EventStoreName eventStore) =>
            ValueTask.FromResult(ObserverRoutingTable.From(new ObserverDefinitionRegistrySnapshot(1, spec._definitions.ToFrozenDictionary(_ => _.Identifier))));

        public Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision) =>
            Task.FromResult(revision);
    }
}
