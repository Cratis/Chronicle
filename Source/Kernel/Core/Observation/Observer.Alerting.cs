// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
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
        try
        {
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
                PartitionsEndedAs = partitionsEndedAs,
                QuarantineEndedAs = quarantineEndedAs
            };
            await GrainFactory.GetGrain<IObserverAlerts>(_observerKey).Reconcile(snapshot);
        }
        catch (Exception exception)
        {
            logger.AlertStateReportingFailed(_observerKey, exception);
        }
    }

    async Task ReportAlertsRemoved()
    {
        try
        {
            await GrainFactory.GetGrain<IObserverAlerts>(_observerKey).Removed();
        }
        catch (Exception exception)
        {
            logger.AlertStateReportingFailed(_observerKey, exception);
        }
    }
}
