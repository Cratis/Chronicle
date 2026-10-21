// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// Describes one explicit scenario delivery turn.
/// </summary>
public enum OccurrenceDeliveryOutcome
{
    /// <summary>
    /// The handler succeeded and the lease completed.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The handler failed and retry or quarantine was recorded.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// The attempt's epoch was superseded.
    /// </summary>
    LeaseLost = 2,

    /// <summary>
    /// There was no claimable occurrence.
    /// </summary>
    NothingDue = 3,

    /// <summary>
    /// The handler succeeded but its lease was deliberately left uncompleted.
    /// </summary>
    Delivered = 4
}
