// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static bool HasExhaustedAutomaticRetries(FailedPartition failure, int maximumAttempts) =>
        failure.IsQuarantined && failure.AttemptsInCurrentBudget >= maximumAttempts;

    bool CanStartRecovery() => !IsRetired && !_removed && !IsQuarantined && KeyOwnsDefinition;

    async Task<(PartitionRecoveryOutcome Outcome, bool StartFailed)> TryRecoverFailedPartitionForOperator(Key partition)
    {
        var releasedFailure = await ReleaseAutomaticRetryLimit(partition);
        var jobStarted = false;
        try
        {
            return await TryRecoverFailedPartition(partition, () => jobStarted = true);
        }
        finally
        {
            if (releasedFailure is not null && !jobStarted)
            {
                await RestoreExhaustionQuarantine(releasedFailure);
            }
        }
    }

    async Task<FailedPartition?> ReleaseAutomaticRetryLimit(Key partition)
    {
        // Webhook quarantine includes delivery policies other than the client's automatic attempt limit.
        if (!CanStartRecovery())
        {
            return null;
        }
        if (IsWebhookSubscription || !_subscription.IsSubscribed)
        {
            return null;
        }

        var configuration = await GetRetryConfiguration();
        if (configuration.MaxRetryAttempts <= 0)
        {
            return null;
        }

        await _alertMutationLock.WaitAsync();
        try
        {
            if (!Failures.TryGet(partition, out var failure))
            {
                return null;
            }
            if (!HasExhaustedAutomaticRetries(failure, configuration.MaxRetryAttempts))
            {
                return null;
            }

            await PersistReleasedRetryLimit(failure);
            return failure;
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task PersistReleasedRetryLimit(FailedPartition failure)
    {
        // Keep the budget and history: another failure must still exhaust the automatic retry policy.
        failure.IsQuarantined = false;
        ChangeAlertState();
        try
        {
            await WriteFailedPartitions();
        }
        catch
        {
            failure.IsQuarantined = true;
            ChangeAlertState();
            throw;
        }
    }

    async Task RestoreExhaustionQuarantine(FailedPartition failure)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (Failures.TryGet(failure.Partition, out var current) && ReferenceEquals(current, failure))
            {
                current.IsQuarantined = true;
                ChangeAlertState();
                await WriteFailedPartitions();
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }
}
