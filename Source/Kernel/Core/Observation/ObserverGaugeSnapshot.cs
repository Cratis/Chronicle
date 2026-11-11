// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what the observer gauges report for one resident observer.
/// </summary>
/// <param name="RunningState">The running state of the observer.</param>
/// <param name="FailedPartitions">The number of failed partitions, quarantined ones included.</param>
/// <param name="QuarantinedPartitions">The number of quarantined partitions.</param>
public record ObserverGaugeSnapshot(ObserverRunningState RunningState, int FailedPartitions, int QuarantinedPartitions)
{
    /// <summary>
    /// The snapshot of an observer that has not reported anything yet.
    /// </summary>
    public static readonly ObserverGaugeSnapshot Initial = new(ObserverRunningState.Unknown, 0, 0);
}
