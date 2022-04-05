// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Holds log messages for <see cref="EventLogPubSubStore"/>.
/// </summary>
public static partial class EventLogPubStoreLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Reading pub sub state")]
    internal static partial void ReadingState(this ILogger logger);

    [LoggerMessage(1, LogLevel.Trace, "Writing pub sub state")]
    internal static partial void WritingState(this ILogger logger);

    [LoggerMessage(2, LogLevel.Trace, "Clearing pub sub state")]
    internal static partial void ClearingState(this ILogger logger);
}
