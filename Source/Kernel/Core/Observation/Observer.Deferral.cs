// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task PartitionDeferred(Key partition, PartitionDeferral deferral, FailedPartitionId? episodeId = null, Guid? expectedDeferralId = null)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || IsQuarantined || !IsCurrentRecovery(partition, episodeId, expectedDeferralId)) return;
            var exists = Failures.TryGet(partition, out var failure);
            failure ??= new() { Partition = partition };

            // A crash here leaves at worst a harmless wake-up. Never commit a deadline without its wake-up.
            await RegisterDeferralReminder(failure, deferral);
            if (!exists)
            {
                Failures.Partitions = Failures.Partitions.Append(failure).ToArray();
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            _pendingRetryReminders.Remove(failure.Id);
            Failures.Defer(partition, deferral);
            ChangeAlertState();
            await failures.WriteStateAsync();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CanResumeFailedPartition(Key partition, FailedPartitionId? episodeId = null, Guid? deferralId = null, Guid? webhookLifecycleId = null)
    {
        if (IsRetired || _removed || IsQuarantined || !_subscription.IsSubscribed ||
            (webhookLifecycleId is not null && webhookLifecycleId != _alertLifecycleId))
        {
            return false;
        }
        if (!Failures.TryGet(partition, out var failure)) return episodeId is null && deferralId is null;

        // Catch-up and replay do not own the failed episode. Even after its deadline, leave delivery to the
        // episode's recovery job: an unfenced completion must not clear or overwrite the durable resume position.
        if (episodeId is null && deferralId is null && failure.Deferral is { } deferral)
        {
            await RegisterDeferralReminder(failure, deferral);
            return false;
        }

        return IsCurrentRecovery(partition, episodeId, deferralId) && !failure.IsQuarantined &&
            !await ParkExpiredWebhookRetry(failure, await GetRetryConfiguration()) && await IsDeferralDue(failure);
    }

    bool IsCurrentRecovery(Key partition, FailedPartitionId? episodeId, Guid? deferralId)
    {
        if (!Failures.TryGet(partition, out var failure)) return episodeId is null && deferralId is null;
        return (episodeId is null || episodeId == failure.Id) && failure.Deferral?.Id == deferralId;
    }

    async Task<bool> IsDeferralDue(FailedPartition failure)
    {
        if (failure.Deferral is not { } deferral) return true;
        await RegisterDeferralReminder(failure, deferral);
        return deferral.Until <= DateTimeOffset.UtcNow;
    }

    Task<IGrainReminder> RegisterDeferralReminder(FailedPartition failure, PartitionDeferral deferral)
    {
        var delay = deferral.Until - DateTimeOffset.UtcNow;
        if (delay <= TimeSpan.Zero) delay = TimeSpan.FromMilliseconds(1);
        return this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), delay, GetRetryReminderPeriod(delay));
    }

    async Task ReconcilePartitionDeferrals()
    {
        if (IsRetired || _removed) return;
        foreach (var failure in Failures.Partitions.Where(_ => _.Deferral is not null && !_.IsQuarantined).ToArray())
        {
            await RegisterDeferralReminder(failure, failure.Deferral!);
        }
    }
}
