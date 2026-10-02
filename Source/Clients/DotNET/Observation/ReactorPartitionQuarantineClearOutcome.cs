// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes whether the quarantine of a failed observer partition was cleared.
/// </summary>
public enum ReactorPartitionQuarantineClearOutcome
{
    /// <summary>
    /// The server returned an outcome the client does not recognize.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The quarantine was cleared and the retry budget of the partition was reset.
    /// </summary>
    Cleared = 1,

    /// <summary>
    /// The partition was not among the observer's failed partitions.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The partition is failed but not quarantined.
    /// </summary>
    NotQuarantined = 3
}
