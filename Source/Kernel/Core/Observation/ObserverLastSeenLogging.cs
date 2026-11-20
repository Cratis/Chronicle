// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverLastSeenLogging
{
    [LoggerMessage(LogLevel.Warning, "Recording that a client was seen holding the subscription failed; the time is recorded again by a later subscription or watchdog tick")]
    internal static partial void FailedToRecordLastSeen(this ILogger<Observer> logger, Exception exception);
}
