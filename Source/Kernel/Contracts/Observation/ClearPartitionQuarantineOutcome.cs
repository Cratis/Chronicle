// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the outcome of clearing the quarantine of a failed partition.
/// </summary>
public enum ClearPartitionQuarantineOutcome
{
    /// <summary>
    /// The quarantine was cleared and the retry budget of the partition was reset.
    /// </summary>
    Cleared = 0,

    /// <summary>
    /// The partition is not among the observer's known failed partitions, so nothing was changed.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The partition is failed but not quarantined, so nothing was changed.
    /// </summary>
    NotQuarantined = 2
}
