// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (!KeyOwnsDefinition || !_observerExists)
        {
            await UnregisterReminderNamed(reminderName);
            return;
        }

        if (reminderName == WebhookProbeReminderName)
        {
            await ReceiveWebhookProbeReminder();
            return;
        }
        if (reminderName == AlertReminderName)
        {
            await ReconcileAlertsIfNeeded();
            await ReleaseAlertReminderWhenSettled();
            return;
        }

        await ReceivePartitionRetryReminder(reminderName);
    }

    /// <summary>
    /// Gets a retry reminder name in a namespace disjoint from the alert reminder, regardless of the partition key.
    /// </summary>
    /// <param name="failureId">The failure episode to retry.</param>
    /// <returns>The bounded retry reminder name.</returns>
    internal static string PartitionReminderName(FailedPartitionId failureId) => $"cp:{failureId.Value:N}";

    static bool IsRetryReminderFor(FailedPartition partition, string reminderName) =>
        !partition.IsQuarantined &&
        (PartitionReminderName(partition.Id) == reminderName || partition.Partition.ToString() == reminderName);

    async Task ReceivePartitionRetryReminder(string reminderName)
    {
        if (!Failures.Partitions.Any(_ => IsRetryReminderFor(_, reminderName) && _.Deferral is not null))
        {
            await UnregisterReminderNamed(reminderName);
        }

        if (CannotRecoverFromReminder())
        {
            return;
        }

        // Accept raw names registered by main as well as bounded episode-specific retry names.
        foreach (var partition in Failures.Partitions.Where(partition => IsRetryReminderFor(partition, reminderName)))
        {
            await StartRecoverJobForFailedPartition(partition);
        }
    }
}
