// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _alertReconciliationPending;
    bool _alertsRemovalPending;
    bool? _projectionDefinitionExists;
    AlertClearedReason _pendingPartitionsEndedAs = AlertClearedReason.Recovered;
    AlertClearedReason _pendingQuarantineEndedAs = AlertClearedReason.Cleared;

    /// <summary>
    /// Reports the current failure state without waiting for alert evaluation or persistence.
    /// </summary>
    /// <param name="partitionsEndedAs">Why departed partition failures ended.</param>
    /// <param name="quarantineEndedAs">Why observer quarantine ended.</param>
    /// <returns>Awaitable dispatch task.</returns>
    /// <remarks>
    /// Disabled alerts are still reported: the evaluator suppresses raises and escalations, but an already open
    /// incident must still clear when its underlying state ends. Snapshot construction and dispatch failures are
    /// logged, never propagated into observation. ConfigurationForObserverProvider reads in-memory options.
    /// </remarks>
    internal async Task ReportAlertState(
        AlertClearedReason partitionsEndedAs = AlertClearedReason.Recovered,
        AlertClearedReason quarantineEndedAs = AlertClearedReason.Cleared)
    {
        if (_retired)
        {
            return;
        }

        if (!_alertReconciliationPending || partitionsEndedAs != AlertClearedReason.Recovered)
        {
            _pendingPartitionsEndedAs = partitionsEndedAs;
        }

        if (!_alertReconciliationPending || quarantineEndedAs != AlertClearedReason.Cleared)
        {
            _pendingQuarantineEndedAs = quarantineEndedAs;
        }

        if (_deferAlertReports)
        {
            _alertReconciliationPending = true;
            return;
        }
        try
        {
            // Retirement retains the observer definition and quarantine, but Projection.Remove deletes the
            // projection definition. Only an unsubscribed projection with no failures can be skipped this way;
            // a disconnected, still-registered projection must continue reporting its quarantine.
            if (!_subscription.IsSubscribed && !Failures.HasFailedPartitions && Definition.Type == ObserverType.Projection &&
                !(_projectionDefinitionExists ??= await storage.GetEventStore(_observerKey.EventStore).Projections.Has((ProjectionId)_observerId.Value)))
            {
                return;
            }

            var configuration = await configurationProvider.GetFor(_observerKey);
            var partitions = Failures.Partitions.Select(partition =>
            {
                var attempts = partition.Attempts.ToArray();
                var latest = partition.LastAttempt;
                return new FailedPartitionSnapshot(
                    partition.Id,
                    partition.Partition.ToString(),
                    attempts.FirstOrDefault()?.Occurred ?? latest.Occurred,
                    latest.Occurred,
                    attempts.Length,
                    partition.IsQuarantined,
                    latest.Kind,
                    latest.Messages.FirstOrDefault() ?? string.Empty);
            }).ToArray();
            var snapshot = new ObserverAlertSnapshot(
                _observerKey,
                partitions,
                State.RunningState == ObserverRunningState.Quarantined,
                _removed,
                configuration.MaxRetryAttempts)
            {
                PartitionsEndedAs = _pendingPartitionsEndedAs,
                QuarantineEndedAs = _pendingQuarantineEndedAs
            };
            await GrainFactory.GetGrain<IObserverAlerts>(_observerKey).Reconcile(snapshot);
            _alertReconciliationPending = false;
            _pendingPartitionsEndedAs = AlertClearedReason.Recovered;
            _pendingQuarantineEndedAs = AlertClearedReason.Cleared;
        }
        catch (Exception exception)
        {
            _alertReconciliationPending = true;
            logger.AlertStateReportingFailed(_observerKey, exception);
        }
    }

    async Task ReconcileAlertsIfNeeded()
    {
        if (_alertsRemovalPending)
        {
            await ReportAlertsRemoved();
        }
        else if (_alertReconciliationPending && !_retired && !_removed)
        {
            await ReportAlertState(_pendingPartitionsEndedAs, _pendingQuarantineEndedAs);
        }
    }

    async Task ReportAlertsRemoved()
    {
        try
        {
            await GrainFactory.GetGrain<IObserverAlerts>(_observerKey).Removed();
            _alertsRemovalPending = false;
            _alertReconciliationPending = false;
            if (_removed)
            {
                DeactivateOnIdle();
            }
        }
        catch (Exception exception)
        {
            _alertsRemovalPending = true;
            logger.AlertStateReportingFailed(_observerKey, exception);
        }
    }
}
