// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes the result of retrying a failed observer partition.
/// </summary>
public enum ReactorPartitionRetryOutcome
{
    /// <summary>
    /// The server returned an outcome the client does not recognize.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A recovery job was started or resumed.
    /// </summary>
    Started = 1,

    /// <summary>
    /// The partition was not among the observer's failed partitions.
    /// </summary>
    PartitionNotFound = 2,

    /// <summary>
    /// The observer is quarantined and cannot recover the partition.
    /// </summary>
    ObserverQuarantined = 3,

    /// <summary>
    /// The partition is quarantined and cannot be retried until cleared.
    /// </summary>
    PartitionQuarantined = 4
}
