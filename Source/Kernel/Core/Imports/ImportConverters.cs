// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Converts between import storage and wire types.
/// </summary>
internal static class ImportConverters
{
    /// <summary>
    /// Converts a rejection.
    /// </summary>
    /// <param name="rejection">The <see cref="ImportRejection"/>.</param>
    /// <returns>The <see cref="ImportEventRejection"/>.</returns>
    internal static ImportEventRejection ToWire(this ImportRejection rejection) => new(rejection.Index, rejection.EventType, rejection.Reasons.ToArray());

    /// <summary>
    /// Converts a staging result.
    /// </summary>
    /// <param name="result">The <see cref="ImportChunkStagingResult"/>.</param>
    /// <returns>The <see cref="ImportChunkResult"/>.</returns>
    internal static ImportChunkResult ToWire(this ImportChunkStagingResult result) =>
        new(result.ChunkIndex, result.AlreadyStaged, result.EventsReceived, result.EventsStaged, result.Rejections.Select(ToWire).ToArray(), result.Error);

    /// <summary>
    /// Converts an operation result.
    /// </summary>
    /// <param name="result">The <see cref="ImportOperationResult"/>.</param>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <returns>The <see cref="ImportStatus"/>.</returns>
    internal static ImportStatus ToStatus(this ImportOperationResult result, ImportId importId) =>
        new(importId.Value, result.Record?.State ?? ImportState.Failed, result.Error);

    /// <summary>
    /// Converts a record and its staged chunks into progress.
    /// </summary>
    /// <param name="record">The <see cref="ImportRecord"/>.</param>
    /// <param name="chunks">The summaries of the staged chunks.</param>
    /// <param name="limits">The configured import limits.</param>
    /// <returns>The <see cref="ImportProgress"/>.</returns>
    internal static ImportProgress ToProgress(this ImportRecord record, IEnumerable<ImportChunkSummary> chunks, Configuration.Imports limits)
    {
        var summaries = chunks.ToArray();
        var staged = record.State.AcceptsChunks() ? summaries.Sum(_ => (long)_.EventsStaged) : record.EventsStaged;
        return new(
            record.Id.Value,
            record.EventSequenceId.Value,
            record.State,
            record.OnInvalid,
            record.Created,
            record.LastActivity,
            summaries.Select(_ => _.ChunkIndex).ToArray(),
            summaries.Sum(_ => (long)_.EventsReceived),
            staged,
            record.EventsCommitted,
            record.AlreadyAppended,
            record.RejectedCount + (record.State.AcceptsChunks() ? summaries.Sum(_ => (long)_.EventsRejected) : 0),
            record.Rejections.Select(ToWire).ToArray(),
            record.CommittedRanges.Select(_ => new ImportSequenceRange(_.ChunkIndex, _.First, _.Last)).ToArray(),
            record.FirstSequenceNumber,
            record.LastSequenceNumber,
            record.FailureReason,
            limits.MaxChunkEvents,
            limits.MaxChunkBytes);
    }
}
