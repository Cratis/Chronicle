// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The values of how a woken observer found its way back to its cursor, as the resume counter reports them (decision 0034).
/// </summary>
internal static class DormantResumeOutcomes
{
    /// <summary>The observer read what was appended since its cursor and delivered it itself.</summary>
    internal const string Cursor = "cursor";

    /// <summary>The gap was larger than the bound, or could not be read cleanly, so catch-up delivers it.</summary>
    internal const string CatchUp = "catch_up";

    /// <summary>The record named a client that is gone or the state could not be resumed, so the record was dropped.</summary>
    internal const string Stale = "stale_record";
}
