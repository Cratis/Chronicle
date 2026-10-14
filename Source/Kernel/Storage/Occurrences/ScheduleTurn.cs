// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// An atomic, version-fenced materialization turn. The next cursor must apply the registered revision.
/// </summary>
/// <param name="Observed">The cursor used to compute the turn.</param>
/// <param name="Next">The resulting cursor with Version raised by one.</param>
/// <param name="Occurrences">The due occurrences to insert if absent.</param>
/// <param name="SkippedRanges">The skip explanations.</param>
/// <param name="DiscardPendingBeforeRevision">An optional discard directive for pending older revisions.</param>
public record ScheduleTurn(
    ScheduleCursor Observed,
    ScheduleCursor Next,
    IReadOnlyList<Occurrence> Occurrences,
    IReadOnlyList<SkippedRange> SkippedRanges,
    long? DiscardPendingBeforeRevision);
