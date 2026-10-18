// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The recovery state included in a failed partition's owner tuple.
/// </summary>
public enum FailedPartitionRecoveryState
{
    /// <summary>
    /// Waiting for a handler or for recovery to start.
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// Retrying a handler failure.
    /// </summary>
    Retrying = 1,

    /// <summary>
    /// Resuming a handler hold.
    /// </summary>
    Resuming = 2
}
