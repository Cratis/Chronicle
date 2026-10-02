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
    /// Gets the reason for partition incidents whose failed partition is no longer in <see cref="FailedPartitions"/>.
    /// </summary>
    /// <remarks>
    /// Applies to every departed failure episode in this snapshot. ResolveFailedPartition (including replay resolution)
    /// supplies Recovered; ClearFailedPartitions supplies Cleared. Removal or retirement sets IsRemoved, which takes
    /// precedence and clears every incident as Removed. The tracker must retain the reason until the clears are appended.
    /// </remarks>
    public AlertClearedReason PartitionsEndedAs { get; init; } = AlertClearedReason.Recovered;

    /// <summary>
    /// Gets the reason to give when the observer quarantine incident ends because <see cref="IsQuarantined"/> is no
    /// longer true. It is <see cref="AlertClearedReason.Cleared"/> unless the caller knows it was a revival.
    /// </summary>
    /// <remarks>
    /// A fresh subscription supplies Revived; ClearObserverQuarantine supplies Cleared. Quarantine survives reactivation
    /// now that #4426 is fixed, so the caller must continue to report IsQuarantined during reactivation. Unguarded exits
    /// tracked in #4440 clear with the reason the tracker supplies here; the evaluator does not infer an exit reason
    /// from an observer running state. The tracker must retain the reason until the clear is appended.
    /// </remarks>
    public AlertClearedReason QuarantineEndedAs { get; init; } = AlertClearedReason.Cleared;
}
