// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Holds log messages for <see cref="EventLogs"/>.
    /// </summary>
    public static partial class EventLogsLogMessages
    {
        [LoggerMessage(0, LogLevel.Information, "Committing event with '{SequenceNumber}' as sequence number")]
        internal static partial void Committing(this ILogger logger, uint sequenceNumber);

        [LoggerMessage(1, LogLevel.Error, "Problem committing event to storage")]
        internal static partial void CommitFailure(this ILogger logger, Exception exception);
    }
}
