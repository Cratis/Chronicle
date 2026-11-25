// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// The commands an operator gives an observer that run as a job, so they are accepted at once and carried out when the
/// observer can serve them.
/// </summary>
public enum ObserverOperatorCommand
{
    /// <summary>
    /// Replay the observer from the beginning.
    /// </summary>
    Replay = 0,

    /// <summary>
    /// Retry every failed partition of the observer.
    /// </summary>
    RetryFailedPartitions = 1,

    /// <summary>
    /// Move the observer to the tail of its event sequence.
    /// </summary>
    FastForward = 2,

    /// <summary>
    /// Replay one partition of the observer.
    /// </summary>
    ReplayPartition = 3,

    /// <summary>
    /// Retry one failed partition of the observer.
    /// </summary>
    RetryPartition = 4,

    /// <summary>
    /// Clear the quarantine of the observer.
    /// </summary>
    ClearObserverQuarantine = 5,

    /// <summary>
    /// Clear the quarantine of one partition of the observer, optionally retrying it at once.
    /// </summary>
    ClearPartitionQuarantine = 6
}
