// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Imports;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ImportLogMessages
{
    [LoggerMessage(LogLevel.Information, "Import '{ImportId}' was begun for event sequence '{EventSequenceId}'")]
    internal static partial void ImportBegun(this ILogger<Import> logger, ImportId importId, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Information, "Import '{ImportId}' closed staging with {Chunks} chunks and {Events} events and is being committed")]
    internal static partial void ImportCompleting(this ILogger<Import> logger, ImportId importId, int chunks, long events);

    [LoggerMessage(LogLevel.Information, "Import '{ImportId}' completed: {Committed} events committed, {AlreadyAppended} already appended, {Rejected} rejected")]
    internal static partial void ImportCompleted(this ILogger<Import> logger, ImportId importId, long committed, long alreadyAppended, long rejected);

    [LoggerMessage(LogLevel.Information, "Import '{ImportId}' was abandoned: {Reason}")]
    internal static partial void ImportAbandoned(this ILogger<Import> logger, ImportId importId, string reason);

    [LoggerMessage(LogLevel.Warning, "Import '{ImportId}' failed: {Reason}")]
    internal static partial void ImportFailed(this ILogger<Import> logger, ImportId importId, string reason);
}
