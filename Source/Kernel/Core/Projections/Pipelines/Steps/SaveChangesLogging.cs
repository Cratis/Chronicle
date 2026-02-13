// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class SaveChangesLogging
{
    [LoggerMessage(LogLevel.Trace, "Saving result for event with sequence number {SequenceNumber}")]
    internal static partial void SavingResult(this ILogger<SaveChanges> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "No changes to save for event with sequence number {SequenceNumber}")]
    internal static partial void NotSaving(this ILogger<SaveChanges> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Not saving for event with sequence number {SequenceNumber} - event was deferred")]
    internal static partial void NotSavingDueToDeferred(this ILogger<SaveChanges> logger, ulong sequenceNumber);
}
