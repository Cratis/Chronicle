// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly Dictionary<Key, RecoveryStart> _recoveryStarts = [];

    /// <summary>
    /// Check whether the recovery job for the current failure episode and deferral of a partition was already started.
    /// </summary>
    /// <param name="failure">The <see cref="FailedPartition"/>.</param>
    /// <param name="window">How long a started recovery counts as in flight; <see cref="TimeSpan.Zero"/> means until it finishes, a new deferral, or the failure is removed.</param>
    /// <returns>True if a recovery job was already started for it, false if not.</returns>
    /// <remarks>
    /// The record is in memory only. It stops matching on a new deferral, and it is lost on reactivation, where the
    /// deferral reminders are registered again.
    /// </remarks>
    bool RecoveryAlreadyStarted(FailedPartition failure, TimeSpan window)
    {
        if (!_recoveryStarts.TryGetValue(failure.Partition, out var start)) return false;
        if (start.Episode == failure.Id && start.Deferral == failure.Deferral?.Id)
        {
            if (window <= TimeSpan.Zero || _clock.GetUtcNow() - start.StartedAt < window) return true;
        }

        ForgetRecoveryStart(failure.Partition);
        return false;
    }

    async Task<bool> RecoveryInFlight(FailedPartition failure)
    {
        if (!RecoveryAlreadyStarted(failure, await GetRecoveryStartWindow())) return false;
        logger.RecoveryAlreadyStarted(failure.Partition);
        return true;
    }

    async Task StartTrackedRecoveryJob(FailedPartition failure, RetryFailedPartitionRequest request)
    {
        // A listing that fails comes back empty and would start a duplicate, so a webhook records its own start up front.
        RecordRecoveryStart(failure);
        var jobId = JobId.NotSet;
        try
        {
            jobId = await StartRecoveryJob(request);
        }
        finally
        {
            if (jobId == JobId.NotSet) ForgetRecoveryStart(failure.Partition);
        }
    }

    void RecordRecoveryStart(FailedPartition failure) =>
        _recoveryStarts[failure.Partition] = new(failure.Id, failure.Deferral?.Id, _clock.GetUtcNow());

    void ForgetRecoveryStart(Key partition) => _recoveryStarts.Remove(partition);

    async Task<TimeSpan> GetRecoveryStartWindow()
    {
        var subscriberTimeout = await configurationProvider.GetSubscriberTimeoutForObserver(_observerKey);
        if (subscriberTimeout <= TimeSpan.Zero) return TimeSpan.Zero;
        var doubled = subscriberTimeout * 2;
        return doubled > _minimumRetryReminderPeriod ? doubled : _minimumRetryReminderPeriod;
    }

    sealed record RecoveryStart(FailedPartitionId Episode, Guid? Deferral, DateTimeOffset StartedAt);
}

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Not starting another recovery for partition {Partition}; one was already started for this failure and deferral")]
    internal static partial void RecoveryAlreadyStarted(this ILogger<Observer> logger, Key partition);
}
