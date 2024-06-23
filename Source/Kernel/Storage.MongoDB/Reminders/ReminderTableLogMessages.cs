// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Reminders;

/// <summary>
/// Holds log messages for <see cref="ReminderTable"/>.
/// </summary>
internal static partial class ReminderTableLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Upserting reminder with key '{Key}'")]
    internal static partial void Upserting(this ILogger<ReminderTable> logger, string key);

    [LoggerMessage(LogLevel.Trace, "Removing reminder with key '{Key}'")]
    internal static partial void Removing(this ILogger<ReminderTable> logger, string key);

    [LoggerMessage(LogLevel.Critical, "Failed upserting reminder with key '{Key}'")]
    internal static partial void FailedUpserting(this ILogger<ReminderTable> logger, string key, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Failed removing reminder with key '{Key}'")]
    internal static partial void FailedRemoving(this ILogger<ReminderTable> logger, string key, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Reading all reminders for '{Key}'")]
    internal static partial void ReadingAllRemindersForGrain(this ILogger<ReminderTable> logger, string key);

    [LoggerMessage(LogLevel.Trace, "Reading reminder '{ReminderName}' for '{Grain}'")]
    internal static partial void ReadingSpecificReminderForGrain(this ILogger<ReminderTable> logger, string grain, string reminderName);

    [LoggerMessage(LogLevel.Trace, "Reading reminders in range '{Begin}' for '{End}'")]
    internal static partial void ReadingRemindersInRange(this ILogger<ReminderTable> logger, uint begin, uint end);
}
