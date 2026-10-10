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
}
