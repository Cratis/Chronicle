// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Alerts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.Extensions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_durable_alert_history : an_observer
{
    protected readonly List<object> _history = [];
    protected ObserverAlerts _tracker;
    protected ObserverRemover _remover;
    protected bool _loseNextClearResponse;
    protected bool _delayNextRaise;
    protected AlertRaised _pendingRaise;
    protected EventSequences.IEventSequence _systemSequence;
    IStorage _trackerStorage;
    IEventSerializer _serializer;
    object _serialized;

    async Task Establish()
    {
        _trackerStorage = Substitute.For<IStorage>();
        _trackerStorage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers.Get(_observerId)
            .Returns(_ => _eventStoreNamespaceStorage.Observers.Get(_observerId));
        var sequenceStorage = Substitute.For<IEventSequenceStorage>();
        _trackerStorage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System).Returns(sequenceStorage);
        sequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(call =>
        {
            var events = _history.Select((transition, index) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(transition.GetType().GetEventType(), (ulong)index))
                .Where(@event => @event.Context.SequenceNumber >= call.Arg<EventSequenceNumber>()).ToArray();
            var cursor = Substitute.For<IEventCursor>();
            cursor.MoveNext().Returns(events.Length > 0, false);
            cursor.Current.Returns(events);
            return cursor;
        });
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(call => _history[(int)call.Arg<AppendedEvent>().Context.SequenceNumber.Value]);
        _serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            _serialized = call.Arg<object>();
            return new JsonObject();
        });
        _systemSequence = Substitute.For<EventSequences.IEventSequence>();
        _systemSequence.Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(_ =>
        {
            if (_delayNextRaise && _serialized is AlertRaised raised)
            {
                _delayNextRaise = false;
                _pendingRaise = raised;
                return Task.FromException<AppendResult>(new TimeoutException("Append still executing on the sequence"));
            }

            _history.Add(_serialized);
            if (_loseNextClearResponse && _serialized is AlertCleared)
            {
                _loseNextClearResponse = false;
                return Task.FromException<AppendResult>(new TimeoutException());
            }
            return Task.FromResult(AppendResult.Success(CorrelationId.NotSet, (ulong)(_history.Count - 1)));
        });
        await CrashTracker();
        _observerAlerts.Configure().Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call => _tracker.Reconcile(call.Arg<ObserverAlertSnapshot>()));

        var grains = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        grains.GetGrain<INamespaces>(_observerKey.EventStore).Returns(namespaces);
        grains.GetGrain<IObserver>(_observerKey).Returns(_ => _observer);
        _eventStoreStorage.Observers.Has(_observerId).Returns(true);
        _remover = new(grains, _storage, NullLogger<ObserverRemover>.Instance);
    }

    protected async Task CrashTracker()
    {
        var silo = new TestKitSilo();
        silo.AddService(_trackerStorage);
        silo.AddService(_serializer);
        silo.AddService(TimeProvider.System);
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions());
        silo.AddService(new ObserverAlertEvaluator(new AlertConditions(options, NullLogger<AlertConditions>.Instance)));
        silo.AddService(NullLogger<ObserverAlerts>.Instance);
        silo.AddKeyedService<IMeter<ObserverAlerts>>(WellKnown.MeterName, new Meter<ObserverAlerts>(ObserverMetricsRecorder.SharedMeter));
        silo.AddProbe(_ => _systemSequence);
        _tracker = await silo.CreateGrainAsync<ObserverAlerts>(_observerKey);
    }

    protected async Task GivenFailingPartitions(int count)
    {
        await PersistFailingPartitions(count);
        await ReportAlerts();
        await ReportAlerts();
        _history.OfType<AlertRaised>().Count().ShouldEqual(count);
    }

    protected async Task PersistFailingPartitions(int count)
    {
        var occurred = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        _failedPartitionsState.Partitions = Enumerable.Range(0, count).Select(index => new FailedPartition
        {
            Id = FailedPartitionId.New(),
            ObserverId = _observerId,
            Partition = $"partition-{index}",
            Attempts = [new() { SequenceNumber = 12UL, Occurred = occurred, Messages = ["Failure"], StackTrace = "Stack", Kind = FailureKind.Handling }]
        }).ToArray();
        await _failedPartitionsStorage.WriteStateAsync();
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = count };
    }

    protected Task<ObserverRemovalResult> RemoveThroughCoordinator() => _remover.Remove(_observerKey.EventStore, _observerId, _observerKey.EventSequenceId);
}
