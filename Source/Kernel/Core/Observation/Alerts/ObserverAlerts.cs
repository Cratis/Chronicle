// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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
/// sequence, not the affected observer's store. Pending transitions keep their episode and clear reason until appended;
/// a keep-alive retry timer owns append recovery independently of the observer's lifetime.
/// </remarks>
/// <param name="storage">The storage from which to fold alert history.</param>
/// <param name="eventSerializer">The serializer for alert transitions.</param>
/// <param name="evaluator">The pure observer alert evaluator.</param>
/// <param name="meter">The meter on which append failures are counted.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional clock, defaulting to the system clock.</param>
public partial class ObserverAlerts(
    IStorage storage,
    IEventSerializer eventSerializer,
    ObserverAlertEvaluator evaluator,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverAlerts> meter,
    ILogger<ObserverAlerts> logger,
    TimeProvider? timeProvider = null) : Grain, IObserverAlerts
{
    static readonly EventType[] _transitionTypes = [typeof(AlertRaised).GetEventType(), typeof(AlertEscalated).GetEventType(), typeof(AlertCleared).GetEventType()];
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Queue<object> _pendingTransitions = new();
    readonly HashSet<IncidentId> _knownIncidents = [];
    IReadOnlyCollection<OpenIncident> _openIncidents = [];
    ObserverAlertSnapshot? _lastSnapshot;
    bool _historyLoaded;
    EventSequenceNumber _tail = EventSequenceNumber.BeforeFirst;
    IGrainTimer? _reconciliationTimer;
    TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    ObserverKey Key => ObserverKey.Parse(this.GetPrimaryKeyString());
    EventSourceId EventSource => $"{Key.EventStore}/{Key.Namespace}/{Key}";

    /// <inheritdoc/>
    public async Task Reconcile(ObserverAlertSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        _reconciliationTimer?.Dispose();
        _reconciliationTimer = null;
        try
        {
            await LoadHistory();
            if (!await AppendPendingTransitions())
            {
                ArmRetryTimer();
                return;
            }

            // Complete the previous episode before evaluating a newer snapshot, especially a new quarantine.
            var evaluation = evaluator.Evaluate(snapshot, _openIncidents, _clock.GetUtcNow());
            foreach (var transition in evaluation.Transitions)
            {
                _pendingTransitions.Enqueue(transition);
            }

            if (!await AppendPendingTransitions())
            {
                ArmRetryTimer();
                return;
            }

            _retryDelay = TimeSpan.FromSeconds(5);
            ArmRaiseTimer(evaluation.NextRaiseDue);
        }
        catch (Exception exception)
        {
            _historyLoaded = false;
            logger.ReconciliationFailed(Key, exception);
            RecordFailure();
            ArmRetryTimer();
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
        var knownIncidents = new HashSet<IncidentId>();
        var tail = EventSequenceNumber.BeforeFirst;
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                var transition = eventSerializer.Deserialize(@event);
                if (transition is AlertRaised raised)
                {
                    knownIncidents.Add(raised.IncidentId);
                }

                incidents = new ObserverAlertEvaluation([transition], null).ApplyTo(incidents);
                tail = @event.Context.SequenceNumber;
            }
        }

        _openIncidents = incidents;
        _knownIncidents.Clear();
        _knownIncidents.UnionWith(knownIncidents);
        _tail = tail;
        _historyLoaded = true;
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
