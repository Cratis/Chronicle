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
/// Reconciles authoritative observer levels with durable system-sequence incident history.
/// </summary>
/// <remarks>
/// There is no transition queue or tracker-owned retry work. The observer's durable reminder retries uncertain
/// application and revisits grace deadlines. History is fully folded on first use and refreshed from its tail
/// thereafter; an ambiguous append invalidates the fold. Source authority is read from storage, never by calling
/// the observer (which may be awaiting this grain).
/// </remarks>
/// <param name="storage">The source state and history storage.</param>
/// <param name="eventSerializer">The transition serializer.</param>
/// <param name="evaluator">The pure evaluator.</param>
/// <param name="meter">The failure meter.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional clock.</param>
public class ObserverAlerts(
    IStorage storage,
    IEventSerializer eventSerializer,
    ObserverAlertEvaluator evaluator,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverAlerts> meter,
    ILogger<ObserverAlerts> logger,
    TimeProvider? timeProvider = null) : Grain, IObserverAlerts
{
    internal const int MaximumTransitionsPerReport = 128;
    static readonly TimeSpan _workBudget = TimeSpan.FromSeconds(5);
    static readonly EventType[] _transitionTypes = [typeof(AlertRaised).GetEventType(), typeof(AlertEscalated).GetEventType(), typeof(AlertCleared).GetEventType()];
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Dictionary<IncidentId, OpenIncident> _openIncidents = [];
    bool _historyLoaded;
    EventSequenceNumber _tail = EventSequenceNumber.BeforeFirst;

    ObserverKey Key => ObserverKey.Parse(this.GetPrimaryKeyString());
    EventSourceId EventSource => $"{Key.EventStore}/{Key.Namespace}/{Key}";

    /// <inheritdoc/>
    public async Task<ObserverAlertReceipt> Reconcile(ObserverAlertSnapshot snapshot)
    {
        var started = _clock.GetUtcNow();
        ObserverAlertReceipt Receipt(ObserverAlertReconciliation outcome, Guid? adoption = null) => new(snapshot.LifecycleId, snapshot.Revision, outcome, adoption);
        try
        {
            if (!await IsAuthoritative(snapshot))
            {
                return Receipt(ObserverAlertReconciliation.Superseded);
            }

            await RefreshHistory();
            if (snapshot.IsQuarantined && snapshot.QuarantineEpisodeId is null && snapshot.Disposition == AlertDisposition.Active)
            {
                // Legacy handshake only. No transitions are applied until the source persists the proposed identity.
                var episode = _openIncidents.Values.FirstOrDefault(_ => _.Condition == AlertConditionKind.ObserverQuarantined)?.Id.Value ?? Guid.NewGuid();
                return Receipt(ObserverAlertReconciliation.RetryRequired, episode);
            }

            var evaluation = evaluator.Evaluate(snapshot, _openIncidents.Values, _clock.GetUtcNow());
            var applied = 0;
            foreach (var transition in evaluation.Transitions)
            {
                if (applied == MaximumTransitionsPerReport || _clock.GetUtcNow() - started >= _workBudget)
                {
                    return Receipt(ObserverAlertReconciliation.RetryRequired);
                }

                if (!await IsAuthoritative(snapshot))
                {
                    return Receipt(ObserverAlertReconciliation.Superseded);
                }

                var result = await GrainFactory.GetSystemEventSequence().Append(
                    EventSourceType.Default,
                    EventSource,
                    EventStreamType.All,
                    EventStreamId.Default,
                    transition.GetType().GetEventType(),
                    eventSerializer.Serialize(transition),
                    CorrelationId.New(),
                    [],
                    Identity.System,
                    [],
                    new ConcurrencyScope(_tail, true, null, null, null, _transitionTypes));
                if (!result.IsSuccess)
                {
                    logger.TransitionAppendRejected(Key, transition.GetType().Name, result);
                    RecordFailure();
                    _historyLoaded = false;
                    return Receipt(ObserverAlertReconciliation.RetryRequired);
                }

                _tail = result.SequenceNumber;
                ObserverAlertEvaluation.Apply(transition, _openIncidents);
                applied++;
            }

            return Receipt(await IsAuthoritative(snapshot) ? ObserverAlertReconciliation.Applied : ObserverAlertReconciliation.Superseded);
        }
        catch (Exception exception)
        {
            _historyLoaded = false;
            logger.ReconciliationFailed(Key, exception);
            RecordFailure();
            return Receipt(ObserverAlertReconciliation.RetryRequired);
        }
    }

    async Task<bool> IsAuthoritative(ObserverAlertSnapshot snapshot)
    {
        if (snapshot.Observer != Key || snapshot.LifecycleId == Guid.Empty)
        {
            return false;
        }

        var source = await storage.GetEventStore(Key.EventStore).GetNamespace(Key.Namespace).Observers.Get(Key.ObserverId);
        return source.Identifier == Key.ObserverId &&
            source.AlertLifecycleId == snapshot.LifecycleId &&
            source.AlertRevision == snapshot.Revision &&
            source.AlertDisposition == snapshot.Disposition &&
            source.QuarantineEpisodeId == snapshot.QuarantineEpisodeId;
    }

    async Task RefreshHistory()
    {
        if (!_historyLoaded)
        {
            _openIncidents.Clear();
            _tail = EventSequenceNumber.BeforeFirst;
        }

        var sequence = storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.System);
        using var cursor = await sequence.GetFromSequenceNumber(_tail.IsActualValue ? _tail.Next() : EventSequenceNumber.First, EventSource, eventTypes: _transitionTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                ObserverAlertEvaluation.Apply(eventSerializer.Deserialize(@event), _openIncidents);
                _tail = @event.Context.SequenceNumber;
            }
        }

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
