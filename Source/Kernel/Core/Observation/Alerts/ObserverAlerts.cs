// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Records alert transitions for an observer, rebuilding its in-memory incidents from the system sequence on first use.
/// </summary>
/// <remarks>
/// The event source is <c language="csharp">{store}/{namespace}/{ObserverKey}</c>, where ObserverKey is its existing string grain key
/// (including the observed event sequence). History and appends use the System store's default namespace and system
/// sequence, not the affected observer's store. No separate persisted grain state is needed.
/// </remarks>
/// <param name="storage">The storage from which to fold alert history.</param>
/// <param name="eventSerializer">The serializer for alert transitions.</param>
/// <param name="evaluator">The pure observer alert evaluator.</param>
/// <param name="meter">The meter on which append failures are counted.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional clock, defaulting to the system clock.</param>
public class ObserverAlerts(
    IStorage storage,
    IEventSerializer eventSerializer,
    ObserverAlertEvaluator evaluator,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverAlerts> meter,
    ILogger<ObserverAlerts> logger,
    TimeProvider? timeProvider = null) : Grain, IObserverAlerts
{
    static readonly EventType[] _transitionTypes = [typeof(AlertRaised).GetEventType(), typeof(AlertEscalated).GetEventType(), typeof(AlertCleared).GetEventType()];
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Dictionary<IncidentId, AlertClearedReason> _pendingClearReasons = [];
    IReadOnlyCollection<OpenIncident> _openIncidents = [];
    ObserverAlertSnapshot? _lastSnapshot;
    bool _historyLoaded;
    EventSequenceNumber _tail = EventSequenceNumber.BeforeFirst;
    IGrainTimer? _raiseTimer;

    ObserverKey Key => ObserverKey.Parse(this.GetPrimaryKeyString());
    EventSourceId EventSource => $"{Key.EventStore}/{Key.Namespace}/{Key}";

    /// <inheritdoc/>
    public async Task Reconcile(ObserverAlertSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        _raiseTimer?.Dispose();
        _raiseTimer = null;
        try
        {
            await LoadHistory();
            var evaluation = evaluator.Evaluate(snapshot, _openIncidents, _clock.GetUtcNow());
            foreach (var cleared in evaluation.Transitions.OfType<AlertCleared>())
            {
                if (snapshot.IsRemoved)
                {
                    _pendingClearReasons[cleared.IncidentId] = AlertClearedReason.Removed;
                }
                else
                {
                    _pendingClearReasons.TryAdd(cleared.IncidentId, cleared.Reason);
                }
            }

            foreach (var transition in evaluation.Transitions)
            {
                var toAppend = transition is AlertCleared cleared
                    ? cleared with { Reason = _pendingClearReasons[cleared.IncidentId] }
                    : transition;
                var result = await GrainFactory.GetSystemEventSequence().Append(
                    EventSourceType.Default,
                    EventSource,
                    EventStreamType.All,
                    EventStreamId.Default,
                    toAppend.GetType().GetEventType(),
                    eventSerializer.Serialize(toAppend),
                    CorrelationId.New(),
                    [],
                    Identity.System,
                    [],
                    new ConcurrencyScope(_tail, true, null, null, null, _transitionTypes));
                if (!result.IsSuccess)
                {
                    logger.TransitionAppendRejected(Key, toAppend.GetType().Name, result);
                    RecordFailure();

                    // Also reload after an ambiguous result or failover: the append may already be durable.
                    _historyLoaded = false;
                    break;
                }

                _tail = result.SequenceNumber;
                _openIncidents = new ObserverAlertEvaluation([toAppend], null).ApplyTo(_openIncidents);
                if (toAppend is AlertCleared appendedClear)
                {
                    _pendingClearReasons.Remove(appendedClear.IncidentId);
                }
            }

            ArmRaiseTimer(evaluation.NextRaiseDue);
        }
        catch (Exception exception)
        {
            _historyLoaded = false;
            logger.ReconciliationFailed(Key, exception);
            RecordFailure();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasOpenIncidents()
    {
        await LoadHistory();
        return _openIncidents.Count > 0;
    }

    /// <inheritdoc/>
    public Task Removed() => Reconcile(new(Key, [], false, true, 0));

    async Task LoadHistory()
    {
        if (_historyLoaded)
        {
            return;
        }

        var sequence = storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System);
        using var cursor = await sequence.GetFromSequenceNumber(EventSequenceNumber.First, EventSource, eventTypes: _transitionTypes);
        IReadOnlyCollection<OpenIncident> incidents = [];
        var tail = EventSequenceNumber.BeforeFirst;
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                incidents = new ObserverAlertEvaluation([eventSerializer.Deserialize(@event)], null).ApplyTo(incidents);
                tail = @event.Context.SequenceNumber;
            }
        }

        _openIncidents = incidents;
        _tail = tail;
        _historyLoaded = true;
        foreach (var id in _pendingClearReasons.Keys.Where(id => incidents.All(incident => incident.Id != id)).ToArray())
        {
            _pendingClearReasons.Remove(id);
        }
    }

    void ArmRaiseTimer(DateTimeOffset? due)
    {
        if (due is null)
        {
            return;
        }

        var delay = due.Value - _clock.GetUtcNow();
        _raiseTimer = this.RegisterGrainTimer(
            _ => Reconcile(_lastSnapshot!),
            new GrainTimerCreationOptions
            {
                DueTime = delay > TimeSpan.Zero ? delay : TimeSpan.Zero,
                Period = Timeout.InfiniteTimeSpan
            });
    }

    void RecordFailure()
    {
        using var scope = meter.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = Key.ObserverId,
            ["EventStore"] = Key.EventStore,
            ["Namespace"] = Key.Namespace,
            ["EventSequenceId"] = Key.EventSequenceId
        });
        scope.TransitionFailed();
    }
}
