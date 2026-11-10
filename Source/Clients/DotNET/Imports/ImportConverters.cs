// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Converts the import contracts to their client types.
/// </summary>
internal static class ImportConverters
{
    /// <summary>
    /// Converts progress.
    /// </summary>
    /// <param name="progress">The <see cref="Contracts.Imports.ImportProgressResponse"/>.</param>
    /// <returns>The <see cref="ImportProgress"/>.</returns>
    internal static ImportProgress ToProgress(this Contracts.Imports.ImportProgressResponse progress) => new(
        progress.ImportId,
        (ImportState)progress.State,
        progress.EventsReceived,
        progress.EventsStaged,
        progress.EventsCommitted,
        progress.AlreadyAppended,
        progress.RejectedCount);

    /// <summary>
    /// Converts progress to a result.
    /// </summary>
    /// <param name="progress">The <see cref="Contracts.Imports.ImportProgressResponse"/>.</param>
    /// <returns>The <see cref="ImportResult"/>.</returns>
    internal static ImportResult ToResult(this Contracts.Imports.ImportProgressResponse progress) => new()
    {
        ImportId = progress.ImportId,
        State = (ImportState)progress.State,
        EventsStaged = progress.EventsStaged,
        EventsCommitted = progress.EventsCommitted,
        AlreadyAppended = progress.AlreadyAppended,
        FirstSequenceNumber = progress.FirstSequenceNumber is { } first ? new EventSequenceNumber(first) : null,
        LastSequenceNumber = progress.LastSequenceNumber is { } last ? new EventSequenceNumber(last) : null,
        RejectedCount = progress.RejectedCount,
        Rejections = progress.Rejections.Select(_ => new ImportRejection(_.Index, _.EventType, [.. _.Reasons])).ToArray(),
        FailureReason = progress.FailureReason
    };
}
