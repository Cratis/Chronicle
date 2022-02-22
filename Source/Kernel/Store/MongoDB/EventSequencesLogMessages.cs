// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Holds log messages for <see cref="EventSequences"/>.
/// </summary>
public static partial class EventSequencesLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Appending event with '{SequenceNumber}' as sequence number")]
    internal static partial void Appending(this ILogger logger, ulong sequenceNumber);

    [LoggerMessage(1, LogLevel.Error, "Problem appending event to storage")]
    internal static partial void AppendFailure(this ILogger logger, Exception exception);
}
