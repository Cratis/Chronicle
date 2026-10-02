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
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.Extensions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.given;

public class an_alert_tracker : Specification
{
    protected readonly ObserverMetricsRecorder _metrics = new();
    protected readonly ControllableTimeProvider _clock = new();
    protected TestKitSilo _silo = new();
    protected readonly List<object> _appends = [];
    protected ObserverAlerts _tracker;
    protected ObserverKey _key;
    protected ObserverAlertSnapshot _snapshot;
    protected IStorage _storage;
    protected IEventSequenceStorage _sequenceStorage;
    protected readonly List<AppendedEvent> _history = [];
    readonly Dictionary<EventSequenceNumber, object> _transitions = [];
    protected ObserverState? _source;
    protected ObserverAlertReceipt _receipt;
    protected IEventSequence _sequence;
    protected IEventSerializer _serializer;
    protected ILogger<ObserverAlerts> _logger;
    protected object _serialized;
    protected ConcurrencyScope _lastScope;

    async Task Establish()
    {
        _key = new(_metrics.ObserverId, "store", "namespace", EventSequenceId.Log);
        _snapshot = new(_key, [new(FailedPartitionId.New(), "partition", _clock.Now - TimeSpan.FromMinutes(10), _clock.Now, 1, 1, false, FailureKind.Handling, "Failed")], false, AlertDisposition.Active, 10)
        {
            LifecycleId = Guid.NewGuid(),
            Revision = 1
        };
        _storage = Substitute.For<IStorage>();
        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System).Returns(_sequenceStorage);
        _storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Observers.Get(_key.ObserverId)
            .Returns(_ => _source ?? SourceFor(_snapshot));
        _sequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(call =>
        {
            var events = _history.Where(_ => _.Context.SequenceNumber >= call.Arg<EventSequenceNumber>()).ToArray();
            var cursor = Substitute.For<IEventCursor>();
            cursor.MoveNext().Returns(events.Length > 0, false);
            cursor.Current.Returns(events);
            return cursor;
        });
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(call => _transitions[call.Arg<AppendedEvent>().Context.SequenceNumber]);
        _serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            _serialized = call.Arg<object>();
            return new JsonObject();
        });
        _sequence = Substitute.For<IEventSequence>();
        _sequence.Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(call =>
        {
            _lastScope = call.Arg<ConcurrencyScope>();
            _appends.Add(_serialized);
            RecordDurable(_serialized);
            return AppendResult.Success(CorrelationId.NotSet, _history[^1].Context.SequenceNumber);
        });
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions());
        var evaluator = new ObserverAlertEvaluator(new AlertConditions(options, NullLogger<AlertConditions>.Instance));
        _logger = Substitute.For<ILogger<ObserverAlerts>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        await CreateTracker(evaluator);
    }

    protected async Task CrashTracker()
    {
        // Discard the entire activation without calling graceful deactivation.
        _silo = new();
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions());
        await CreateTracker(new ObserverAlertEvaluator(new AlertConditions(options, NullLogger<AlertConditions>.Instance)));
    }

    protected static ObserverState SourceFor(ObserverAlertSnapshot snapshot) => new()
    {
        Identifier = snapshot.Observer.ObserverId,
        AlertLifecycleId = snapshot.LifecycleId,
        AlertRevision = snapshot.Revision,
        AlertDisposition = snapshot.Disposition,
        QuarantineEpisodeId = snapshot.QuarantineEpisodeId
    };

    async Task CreateTracker(ObserverAlertEvaluator evaluator)
    {
        _silo.AddService(_storage);
        _silo.AddService(_serializer);
        _silo.AddService(evaluator);
        _silo.AddService(_logger);
        _silo.AddService<TimeProvider>(_clock);
        _silo.AddKeyedService<IMeter<ObserverAlerts>>(WellKnown.MeterName, new Meter<ObserverAlerts>(ObserverMetricsRecorder.SharedMeter));
        _silo.AddProbe(_ => _sequence);
        _tracker = await _silo.CreateGrainAsync<ObserverAlerts>(_key);
    }

    protected Task<ObserverAlertReceipt> ReconcileRemoval()
    {
        _snapshot = _snapshot with { Disposition = AlertDisposition.Retired };
        return _tracker.Reconcile(_snapshot);
    }

    protected void GivenHistory(params object[] transitions)
    {
        _history.Clear();
        _transitions.Clear();
        foreach (var transition in transitions)
        {
            RecordDurable(transition);
        }
    }

    protected void RecordDurable(object transition)
    {
        var sequence = _history.Count == 0 ? EventSequenceNumber.First : _history[^1].Context.SequenceNumber.Next();
        var appended = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(transition.GetType().GetEventType(), sequence);
        _transitions[sequence] = transition;
        _history.Add(appended);
    }

    protected void AppendReturns(AppendResult result) => _sequence.Configure().Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(result);

    protected void AppendUsing(Func<AppendResult> append) => _sequence.Configure().Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(_ => append());

    protected void AppendSucceedsFrom(ulong nextSequenceNumber) => _sequence.Configure().Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(call =>
    {
        _lastScope = call.Arg<ConcurrencyScope>();
        _appends.Add(_serialized);
        RecordDurable(_serialized);
        return AppendResult.Success(CorrelationId.NotSet, nextSequenceNumber++);
    });

    protected AlertRaised RaisedForSnapshot() => new(_snapshot.FailedPartitions.Single().Id, Concepts.Alerts.AlertConditionKind.PartitionFailing, Concepts.Alerts.AlertSeverity.Warning, AlertTarget.For(_key, "partition"), AlertEvidence.Create(1, _clock.Now, _clock.Now, FailureKind.Handling, "Failed"));

    void Destroy() => _metrics.Dispose();
}
