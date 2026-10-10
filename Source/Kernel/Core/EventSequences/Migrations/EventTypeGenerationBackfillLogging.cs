// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

internal static partial class EventTypeGenerationBackfillLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Appended generation {Generation} is missing for event {SequenceNumber}; deriving from the highest stored base generation")]
    internal static partial void AppendedContentMissing(this ILogger logger, EventSequenceNumber sequenceNumber, EventTypeGeneration generation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping backfill for event {SequenceNumber} of type {EventTypeId} after two concurrent write conflicts")]
    internal static partial void BackfillConflicted(this ILogger logger, EventSequenceNumber sequenceNumber, EventTypeId eventTypeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping backfill for event {SequenceNumber} of type {EventTypeId}; source generation {Generation} is not in the event type definition")]
    internal static partial void SourceGenerationNotDefined(this ILogger logger, EventSequenceNumber sequenceNumber, EventTypeId eventTypeId, EventTypeGeneration generation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generation backfill completed successfully for event type {EventTypeId}")]
    internal static partial void BackfillCompleted(this ILogger logger, EventTypeId eventTypeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Generation backfill completed with failures for event type {EventTypeId}")]
    internal static partial void BackfillFailed(this ILogger logger, EventTypeId eventTypeId);
}
