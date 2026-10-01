// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts;

public partial class ObserverAlerts
{
    async Task<bool> AppendPendingTransitions()
    {
        while (_pendingTransitions.TryPeek(out var transition))
        {
            // An ambiguous append or concurrency rejection may already have committed this transition.
            if (!IsRecorded(transition))
            {
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
                    return false;
                }

                _tail = result.SequenceNumber;
                _openIncidents = new ObserverAlertEvaluation([transition], null).ApplyTo(_openIncidents);
                if (transition is AlertRaised raised)
                {
                    _knownIncidents.Add(raised.IncidentId);
                }
            }

            _pendingTransitions.Dequeue();
        }

        return true;
    }

    bool IsRecorded(object transition) => transition switch
    {
        AlertRaised raised => _knownIncidents.Contains(raised.IncidentId),
        AlertCleared cleared => _openIncidents.All(incident => incident.Id != cleared.IncidentId),
        AlertEscalated escalated => !_openIncidents.Any(incident => incident.Id == escalated.IncidentId && incident.Condition != escalated.Condition),
        _ => false
    };

    void ArmRetryTimer()
    {
        ArmReconciliationTimer(_retryDelay, reloadHistory: true);
        _retryDelay = TimeSpan.FromSeconds(Math.Min(_retryDelay.TotalSeconds * 2, 300));
    }

    void ArmRaiseTimer(DateTimeOffset? due)
    {
        if (due is not null)
        {
            var delay = due.Value - _clock.GetUtcNow();
            ArmReconciliationTimer(delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
        }
    }

    void ArmReconciliationTimer(TimeSpan delay, bool reloadHistory = false) => _reconciliationTimer = this.RegisterGrainTimer(
        async _ =>
        {
            if (reloadHistory)
            {
                _historyLoaded = false;
            }

            await Reconcile(_lastSnapshot!);
        },
        new GrainTimerCreationOptions
        {
            DueTime = delay,
            Period = Timeout.InfiniteTimeSpan,
            KeepAlive = true
        });
}
