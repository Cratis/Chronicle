// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the outcome of asking an observer to recover a specific failed partition.
/// </summary>
/// <remarks>
/// A manual recovery request used to always report success, even when nothing was actually attempted - the observer
/// was quarantined, the partition was not among its known failures, or the partition itself was quarantined. An
/// operator retrying a partition deserves to know which of those happened rather than being told the retry worked
/// when it silently did nothing.
/// </remarks>
public enum PartitionRecoveryOutcome
{
    /// <summary>
    /// A recovery job was started or resumed for the partition.
    /// </summary>
    Started = 0,

    /// <summary>
    /// The partition is not among the observer's known failed partitions, so there was nothing to recover.
    /// </summary>
    PartitionNotFound = 1,

    /// <summary>
    /// The observer itself is quarantined, so automatic and manual recovery alike are paused until the quarantine is cleared.
    /// </summary>
    ObserverQuarantined = 2,

    /// <summary>
    /// The partition has exceeded its retry attempts and been quarantined individually, so it will not be retried until explicitly cleared.
    /// </summary>
    PartitionQuarantined = 3
}
