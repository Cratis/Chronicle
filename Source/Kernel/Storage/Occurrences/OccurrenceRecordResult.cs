// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The outcome of insert-if-absent ingress.
/// </summary>
public enum OccurrenceRecordResult
{
    /// <summary>
    /// Recorded.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// AlreadyRecorded.
    /// </summary>
    AlreadyRecorded = 1,

    /// <summary>
    /// OccurrenceIdConflict.
    /// </summary>
    OccurrenceIdConflict = 2
}
