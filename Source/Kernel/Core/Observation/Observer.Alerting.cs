// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Identifies the durable wakeup dedicated to alert reconciliation.
    /// </summary>
    internal const string AlertReminderName = "chronicle-observer-alert-reconciliation";
    readonly SemaphoreSlim _alertMutationLock = new(1, 1);
    readonly SemaphoreSlim _stateWriteLock = new(1, 1);
    readonly Dictionary<IncidentId, AlertClearedReason> _alertEndings = [];
    Guid _alertLifecycleId;
    long _alertRevision;
    AlertDisposition _alertDisposition;
    Guid? _quarantineEpisodeId;
    bool _alertReconciliationPending = true;
    bool _alertStateNeedsPersistence;
    bool _alertReportScheduled;
    bool _alertReportInProgress;
    bool? _projectionDefinitionExists;

    bool IsRetired => _alertDisposition == AlertDisposition.Retired;
    bool IsRemoving => _alertDisposition == AlertDisposition.Removing;

    /// <inheritdoc/>
    public void Dispose()
    {
        _metrics?.Dispose();
        _alertMutationLock.Dispose();
        _stateWriteLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Coalesces immediate reports into a separate turn, after source persistence and state routing settle.
    /// </summary>
    internal void ScheduleAlertReport()
    {
        _alertReconciliationPending = true;
        if (_deferAlertReports || _alertReportScheduled || _removed)
        {
            return;
        }

        _alertReportScheduled = true;
        this.ScheduleInSeparateTurn(async () =>
        {
            _alertReportScheduled = false;
            if (_alertReconciliationPending)
            {
                await ReportAlertState();
            }
        });
    }

    /// <summary>
    /// Applies a committed report. A failure retains dirty state; management callers must require true.
    /// </summary>
    /// <returns>Whether the still-current report was durably applied.</returns>
    internal async Task<bool> ReportAlertState()
    {
        if (_alertReportInProgress || _removed || _deferAlertReports)
        {
            return false;
        }

        _alertReportInProgress = true;
        try
        {
            ObserverAlertSnapshot snapshot;
            await _alertMutationLock.WaitAsync();
            try
            {
                if (_alertStateNeedsPersistence)
                {
                    await WriteStateAsync();
                }

                var namespaceStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace);
                var source = await namespaceStorage.Observers.Get(_observerId);
                if (source.Identifier != _observerId || source.AlertLifecycleId != _alertLifecycleId || source.AlertRevision != _alertRevision)
                {
                    return false;
                }

                var configuration = await configurationProvider.GetFor(_observerKey);
                var committedFailures = await namespaceStorage.FailedPartitions.GetFor(_observerId);
                var quarantineDesired = source.RunningState == ObserverRunningState.Quarantined;
                if (!_subscription.IsSubscribed && Definition.Type == ObserverType.Projection)
                {
                    _projectionDefinitionExists ??= await storage.GetEventStore(_observerKey.EventStore).Projections.Has((ProjectionId)_observerId.Value);
                    quarantineDesired &= _projectionDefinitionExists.Value;
                }

                var partitions = committedFailures.Partitions.Select(partition =>
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
                snapshot = new(_observerKey, partitions, quarantineDesired, source.AlertDisposition, configuration.MaxRetryAttempts)
                {
                    LifecycleId = source.AlertLifecycleId,
                    Revision = source.AlertRevision,
                    QuarantineEpisodeId = source.QuarantineEpisodeId,
                    Endings = _alertEndings.ToImmutableDictionary(_ => _.Key.Value, _ => _.Value)
                };
            }
            finally
            {
                _alertMutationLock.Release();
            }

            var receipt = await GrainFactory.GetGrain<IObserverAlerts>(_observerKey).Reconcile(snapshot);
            await _alertMutationLock.WaitAsync();
            try
            {
                if (receipt.LifecycleId != snapshot.LifecycleId || receipt.Revision != snapshot.Revision ||
                    _alertLifecycleId != snapshot.LifecycleId || _alertRevision != snapshot.Revision)
                {
                    _alertReconciliationPending = true;
                    return false;
                }

                if (receipt.QuarantineEpisodeId is { } adopted && snapshot.IsQuarantined && _quarantineEpisodeId is null)
                {
                    _quarantineEpisodeId = adopted;
                    ChangeAlertState();
                    await WriteStateAsync();
                    return false;
                }

                if (receipt.Outcome != ObserverAlertReconciliation.Applied)
                {
                    _alertReconciliationPending = true;
                    return false;
                }

                _alertReconciliationPending = false;
                foreach (var ending in snapshot.Endings)
                {
                    var incidentId = new IncidentId(ending.Key);
                    if (_alertEndings.TryGetValue(incidentId, out var reason) && reason == ending.Value)
                    {
                        _alertEndings.Remove(incidentId);
                    }
                }

                return true;
            }
            finally
            {
                _alertMutationLock.Release();
            }
        }
        catch (Exception exception)
        {
            _alertReconciliationPending = true;
            logger.AlertStateReportingFailed(_observerKey, exception);
            return false;
        }
        finally
        {
            _alertReportInProgress = false;
        }
    }

    void ChangeAlertState()
    {
        _alertRevision++;
        _alertStateNeedsPersistence = true;
        ScheduleAlertReport();
    }

    async Task InitializeAlertState()
    {
        _alertLifecycleId = State.AlertLifecycleId;
        _alertRevision = State.AlertRevision;
        _alertDisposition = State.AlertDisposition;
        _quarantineEpisodeId = State.QuarantineEpisodeId;

        // Retain this reminder even while healthy. Unregistering on an acknowledgment races interleaved recovery.
        await this.RegisterOrUpdateReminder(AlertReminderName, _minimumRetryReminderPeriod, _minimumRetryReminderPeriod);
        if (_alertLifecycleId == Guid.Empty)
        {
            _alertLifecycleId = Guid.NewGuid();
            _alertRevision++;
            await WriteStateAsync();
        }
    }

    async Task BeginAlertLifecycle()
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            ThrowIfRemoving();
            _alertLifecycleId = Guid.NewGuid();
            _alertRevision = 0;
            _alertDisposition = AlertDisposition.Active;
            _projectionDefinitionExists = null;
            ChangeAlertState();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    void RememberQuarantineEnding(AlertClearedReason reason)
    {
        if (_quarantineEpisodeId is { } episode)
        {
            _alertEndings[new(episode)] = reason;
        }
    }

    void ThrowIfRemoving()
    {
        if (IsRemoving || _removed || State.AlertDisposition == AlertDisposition.Removing)
        {
            throw new ObserverRemovalInProgress(_observerKey);
        }
    }

    async Task ReconcileAlertsIfNeeded()
    {
        if (_alertReconciliationPending || Failures.HasFailedPartitions ||
            (_alertDisposition == AlertDisposition.Active && State.RunningState == ObserverRunningState.Quarantined))
        {
            await ReportAlertState();
        }
    }

    async Task RequireAlertReconciliation()
    {
        if (!await ReportAlertState())
        {
            throw new ObserverAlertsNotReconciled(_observerKey);
        }
    }
}
