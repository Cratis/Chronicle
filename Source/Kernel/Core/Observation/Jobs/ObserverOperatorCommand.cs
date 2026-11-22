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
    RetryFailedPartitions = 1
}
