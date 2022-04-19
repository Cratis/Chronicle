// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Partial Observer implementation focused on the reminder for failed partitions aspect.
/// </summary>
public partial class Observer
{
    /// <inheritdoc/>
    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == RecoverReminder)
        {
            var reminder = await GetReminder(RecoverReminder);
            if (reminder is null)
            {
                return;
            }

            if (!State.HasFailedPartitions)
            {
                await UnregisterReminder(reminder);
                return;
            }

            await TryResumeAnyFailedPartitions();
        }
    }

    async Task HandleReminderRegistration()
    {
        if (!State.HasFailedPartitions && _recoverReminder != default)
        {
            await UnregisterReminder(_recoverReminder);
        }
        if (State.HasFailedPartitions && _recoverReminder == default)
        {
            var anyPartitionedToRetry = State.FailedPartitions.Any(_ => _.Attempts <= 10);
            if (anyPartitionedToRetry)
            {
                _recoverReminder = await RegisterOrUpdateReminder(
                    RecoverReminder,
                    TimeSpan.FromSeconds(60),
                    TimeSpan.FromSeconds(60));
            }
            else
            {
                var reminder = await GetReminder(RecoverReminder);
                if (reminder is not null)
                {
                    await UnregisterReminder(reminder);
                }
                _recoverReminder = null;
            }
        }

        await Task.CompletedTask;
    }
}
