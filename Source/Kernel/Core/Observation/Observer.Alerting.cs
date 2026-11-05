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
    internal const string AlertReminderName = "chronicle-observer:alerts";
    const string LegacyAlertReminderName = "chronicle-observer-alert-reconciliation";
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
    bool _observerExists;
    bool _alertReminderRegistered = true;

    bool IsRetired => _alertDisposition == AlertDisposition.Retired;

    /// <inheritdoc/>
    public void Dispose()
    {
        _webhookProbeCancellation?.Dispose();
        _metrics?.Dispose();
        _coalescedWriteTimer?.Dispose();
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
        EnsureAlertReminderRegistered();
        if (_deferAlertReports || _alertReportScheduled || _removed || !_observerExists)
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
        if (_alertReportInProgress || _removed || _deferAlertReports || !_observerExists)
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

                var retryConfiguration = await GetRetryConfiguration();
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
                        partition.AttemptsInCurrentBudget,
                        partition.IsQuarantined,
                        latest.Kind,
                        latest.Messages.FirstOrDefault() ?? string.Empty);
                }).ToArray();
                snapshot = new(_observerKey, partitions, quarantineDesired, source.AlertDisposition, retryConfiguration.MaxRetryAttempts)
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

        // The state provider supplies an identifier even for missing records. Consult storage rather than
        // treating a management probe (or a shared definition in another namespace) as observer creation.
        var source = await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers.Get(_observerId);
        if (!Failures.Partitions.Any(partition => partition.Partition.ToString() == LegacyAlertReminderName))
        {
            await UnregisterReminderNamed(LegacyAlertReminderName);
        }

        // An activation under another event sequence than the definition's is not this observer, even though it
        // loads its record. It must neither keep the reminder that reactivates it nor count as existing.
        if (source.Identifier != _observerId || !KeyOwnsDefinition)
        {
            await UnregisterReminderNamed(AlertReminderName);
            return;
        }

        await EnsureAlertState();
    }

    /// <summary>
    /// Registers the alert reminder again after a settled observer gave it up.
    /// </summary>
    void EnsureAlertReminderRegistered()
    {
        if (_alertReminderRegistered || !_observerExists || _removed)
        {
            return;
        }

        _alertReminderRegistered = true;
        this.ScheduleInSeparateTurn(() => this.RegisterOrUpdateReminder(AlertReminderName, AlertReminderDueTime, _minimumRetryReminderPeriod));
    }

    /// <summary>
    /// Gets when the alert reminder first fires. Orleans anchors every later tick to this moment, so a due time derived
    /// from the observer key spreads thousands of reminders over the period instead of letting them tick in one second.
    /// </summary>
    TimeSpan AlertReminderDueTime
    {
        get
        {
            // string.GetHashCode is randomized per process, so use a stable hash to keep the phase across restarts.
            var hash = 2166136261u;
            foreach (var character in this.GetPrimaryKeyString())
            {
                hash = (hash ^ character) * 16777619u;
            }

            var offset = TimeSpan.FromTicks(_minimumRetryReminderPeriod.Ticks * (hash % 1000) / 1000);
            return _minimumRetryReminderPeriod + offset;
        }
    }

    /// <summary>
    /// Gets a value indicating whether something is waiting to be reported, which is the only time the reminder is needed.
    /// </summary>
    bool HasAlertWorkPending =>
        _alertReconciliationPending ||
        _alertStateNeedsPersistence ||
        _alertLifecycleId == Guid.Empty ||
        Failures.HasFailedPartitions ||
        State.RunningState == ObserverRunningState.Quarantined;

    /// <summary>
    /// Gives the alert reminder up once a tick finds nothing to report.
    /// </summary>
    /// <remarks>
    /// An observer with nothing wrong does not need to be woken every minute. Thousands of them, all overdue after the
    /// kernel was down, activated at once on start and exhausted the connection pool (#5677). A change that needs
    /// reporting registers the reminder again through <see cref="ScheduleAlertReport"/>.
    /// </remarks>
    async Task ReleaseAlertReminderWhenSettled()
    {
        // A sealed activation keeps its wakeup until reactivation can tell whether the source still exists.
        if (_removed ||
            !_observerExists ||
            _alertReconciliationPending ||
            _alertStateNeedsPersistence ||
            Failures.HasFailedPartitions ||
            State.RunningState == ObserverRunningState.Quarantined)
        {
            return;
        }

        _alertReminderRegistered = false;
        await UnregisterReminderNamed(AlertReminderName);
    }

    async Task EnsureAlertState()
    {
        _observerExists = true;

        // A healthy observer needs no wakeup. The reminder is registered while something is waiting to be reported and
        // released again by ReleaseAlertReminderWhenSettled once a tick finds nothing.
        if (HasAlertWorkPending)
        {
            _alertReminderRegistered = true;
            await this.RegisterOrUpdateReminder(AlertReminderName, AlertReminderDueTime, _minimumRetryReminderPeriod);
        }
        else
        {
            _alertReminderRegistered = false;
            await UnregisterReminderNamed(AlertReminderName);
        }

        if (_alertLifecycleId == Guid.Empty)
        {
            _alertLifecycleId = Guid.NewGuid();
            _alertRevision++;
            await WriteStateAsync();
        }
    }

    async Task BeginAlertLifecycle()
    {
        // Subscribe re-reads storage; do not publish Active until every retained, possibly already-ended
        // episode is discarded. A failure in preparation leaves the committed level Retired.
        _alertDisposition = State.AlertDisposition;
        _alertLifecycleId = State.AlertLifecycleId;
        _alertRevision = State.AlertRevision;
        _quarantineEpisodeId = State.QuarantineEpisodeId;
        if (IsRetired)
        {
            await DiscardFailedPartitions(AlertClearedReason.Removed);
            await LeaveQuarantineForSubscription();
        }

        await _alertMutationLock.WaitAsync();
        try
        {
            ThrowIfSealed();
            await EnsureAlertState();
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

    async Task CommitRetired()
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!_observerExists) return;
            if (!IsRetired)
            {
                _alertDisposition = AlertDisposition.Retired;
                ChangeAlertState();
            }

            // A previous persistence failure may have changed memory without committing the desired level.
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    void ThrowIfSealed()
    {
        if (_removed)
        {
            throw new ObserverActivationSealed(_observerKey);
        }
    }

    async Task ReconcileAlertsIfNeeded()
    {
        if (!_observerExists)
        {
            await UnregisterReminderNamed(AlertReminderName);
            return;
        }

        // A sealed activation may have observed an ambiguous deletion failure. Keep its wakeup until
        // reactivation can determine whether the source still exists.
        if (_removed) return;

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
