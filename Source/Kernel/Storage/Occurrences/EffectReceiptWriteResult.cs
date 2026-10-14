// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The outcome of an atomic receipt write.
/// </summary>
public enum EffectReceiptWriteResult
{
    /// <summary>
    /// Written.
    /// </summary>
    Written = 0,

    /// <summary>
    /// AlreadyRecorded.
    /// </summary>
    AlreadyRecorded = 1,

    /// <summary>
    /// DuplicateEventSequenceNumber.
    /// </summary>
    DuplicateEventSequenceNumber = 2,

    /// <summary>
    /// TransactionConflicted.
    /// </summary>
    TransactionConflicted = 3
}
