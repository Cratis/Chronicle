// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents what the kernel knows about an observer that is relevant to alerting, at one point in time.
/// </summary>
/// <remarks>
/// The snapshot says what is true, not what changed. The evaluator compares it with the incidents that are open and
/// works out the transitions, which is what makes duplicate notifications and a missed one harmless.
/// </remarks>
/// <param name="Observer">The <see cref="ObserverKey"/> of the observer.</param>
/// <param name="FailedPartitions">The partitions of the observer that are still failing.</param>
/// <param name="IsQuarantined">Whether the observer as a whole is quarantined.</param>
/// <param name="IsRemoved">Whether the observer has been removed.</param>
/// <param name="MaxRetryAttempts">The configured maximum number of retries of a failed partition, where 0 means retry forever.</param>
public record ObserverAlertSnapshot(
    ObserverKey Observer,
    IReadOnlyCollection<FailedPartitionSnapshot> FailedPartitions,
    bool IsQuarantined,
    bool IsRemoved,
    int MaxRetryAttempts)
{
    /// <summary>
    /// Gets the reason to give when the observer quarantine incident ends because <see cref="IsQuarantined"/> is no
    /// longer true. It is <see cref="AlertClearedReason.Cleared"/> unless the caller knows it was a revival.
    /// </summary>
    public AlertClearedReason QuarantineEndedAs { get; init; } = AlertClearedReason.Cleared;
}
