// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Copy;

internal static partial class CopyEventSourceEventsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Copied events from event source {SourceId} to {TargetId} in event sequence {EventSequenceId}")]
    internal static partial void CopyCompleted(this ILogger logger, EventSourceId sourceId, EventSourceId targetId, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Warning, "Copying events from event source {SourceId} to {TargetId} in event sequence {EventSequenceId} completed with failures")]
    internal static partial void CopyFailed(this ILogger logger, EventSourceId sourceId, EventSourceId targetId, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Information, "Copying {Count} events from event source {SourceId} to {TargetId}")]
    internal static partial void CopyingBatch(this ILogger logger, int count, EventSourceId sourceId, EventSourceId targetId);
}
