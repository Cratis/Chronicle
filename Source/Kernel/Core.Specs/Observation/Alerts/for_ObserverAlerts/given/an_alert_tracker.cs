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
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.given;

public class an_alert_tracker : Specification
{
    protected readonly ObserverMetricsRecorder _metrics = new();
    protected readonly ControllableTimeProvider _clock = new();
    protected readonly TestKitSilo _silo = new();
    protected readonly List<object> _appends = [];
    protected ObserverAlerts _tracker;
    protected ObserverKey _key;
    protected ObserverAlertSnapshot _snapshot;
    protected IStorage _storage;
    protected IEventSequenceStorage _sequenceStorage;
    protected IEventCursor _cursor;
    protected IEventSequence _sequence;
    protected IEventSerializer _serializer;
    protected ILogger<ObserverAlerts> _logger;
    protected object _serialized;
    protected ConcurrencyScope _lastScope;

    async Task Establish()
    {
        _key = new(_metrics.ObserverId, "store", "namespace", EventSequenceId.Log);
        _snapshot = new(_key, [new(FailedPartitionId.New(), "partition", _clock.Now - TimeSpan.FromMinutes(10), _clock.Now, 1, false, FailureKind.Handling, "Failed")], false, false, 10);
        _storage = Substitute.For<IStorage>();
        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System).Returns(_sequenceStorage);
        _cursor = Substitute.For<IEventCursor>();
        _sequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(_cursor);
        _serializer = Substitute.For<IEventSerializer>();
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
            return AppendResult.Success(CorrelationId.NotSet, (ulong)(_appends.Count - 1));
        });
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions());
        var evaluator = new ObserverAlertEvaluator(new AlertConditions(options, NullLogger<AlertConditions>.Instance));
        _logger = Substitute.For<ILogger<ObserverAlerts>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _silo.AddService(_storage);
        _silo.AddService(_serializer);
        _silo.AddService(evaluator);
        _silo.AddService(_logger);
        _silo.AddService<TimeProvider>(_clock);
        _silo.AddKeyedService<IMeter<ObserverAlerts>>(WellKnown.MeterName, new Meter<ObserverAlerts>(ObserverMetricsRecorder.SharedMeter));
        _silo.AddProbe(_ => _sequence);
        _tracker = await _silo.CreateGrainAsync<ObserverAlerts>(_key);
    }

    protected void GivenHistory(params object[] transitions)
    {
        var events = transitions.Select((transition, index) =>
        {
            var appended = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(transition.GetType().GetEventType(), (ulong)index);
            _serializer.Deserialize(appended).Returns(transition);
            return appended;
        }).ToArray();
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns(events);
    }

    protected void AppendReturns(AppendResult result) => _sequence.Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(result);

    protected AlertRaised RaisedForSnapshot() => new(_snapshot.FailedPartitions.Single().Id, Concepts.Alerts.AlertConditionKind.PartitionFailing, Concepts.Alerts.AlertSeverity.Warning, AlertTarget.For(_key, "partition"), AlertEvidence.Create(1, _clock.Now, _clock.Now, FailureKind.Handling, "Failed"));

    void Destroy() => _metrics.Dispose();
}
