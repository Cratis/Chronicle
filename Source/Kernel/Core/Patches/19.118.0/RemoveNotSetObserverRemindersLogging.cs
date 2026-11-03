// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

internal static partial class RemoveNotSetObserverRemindersLogging
{
    [LoggerMessage(LogLevel.Information, "Removed reminder {ReminderName} of {GrainId}, an observer of the event store [NotSet]")]
    internal static partial void RemovedReminder(this ILogger<RemoveNotSetObserverReminders> logger, string reminderName, string grainId);

    [LoggerMessage(LogLevel.Information, "Removed {Count} reminders of observers of the event store [NotSet]")]
    internal static partial void RemovedReminders(this ILogger<RemoveNotSetObserverReminders> logger, int count);
}
