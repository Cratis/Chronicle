// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a timestamped measurement of an observer's progress, taken by the observer itself.
/// </summary>
/// <param name="Status">What the measurement says about the observer.</param>
/// <param name="Cause">Why it says that.</param>
/// <param name="TailEventSequenceNumber">The end of the event sequence when measured, or null when it could not be read.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer expected, or null when it is not an actual position.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled, or null when it has handled none.</param>
/// <param name="RelevantPendingEventSequenceNumber">The first event the observer observes at or after its cursor, or null. Null is not proof there is none: <see cref="Cause"/> says whether it is known.</param>
/// <param name="Distance">How many event sequence slots lie between the cursor and the end of the event sequence, or null when unmeasurable. It counts slots in the sequence, not events the observer will handle, and exactly zero means caught up.</param>
/// <param name="SampledAt">When the measurement was taken, or null when there never was one.</param>
/// <param name="ExpiresAt">When the measurement stops describing the observer, or null when there never was one.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.ObserverLagSample")]
public record ObserverLagSample(
    [Id(0)] ObserverLagStatus Status,
    [Id(1)] ObserverLagCause Cause,
    [Id(2)] ulong? TailEventSequenceNumber,
    [Id(3)] ulong? NextEventSequenceNumber,
    [Id(4)] ulong? LastHandledEventSequenceNumber,
    [Id(5)] ulong? RelevantPendingEventSequenceNumber,
    [Id(6)] ulong? Distance,
    [Id(7)] DateTimeOffset? SampledAt,
    [Id(8)] DateTimeOffset? ExpiresAt)
{
    /// <summary>
    /// Gets the measurement of an observer that has not been measured.
    /// </summary>
    public static ObserverLagSample NotSampled { get; } = Unmeasurable(ObserverLagCause.NoSample);

    /// <summary>
    /// Creates a measurement that says nothing can be said.
    /// </summary>
    /// <param name="cause">Why nothing can be said.</param>
    /// <param name="sampledAt">When it was found out, or null if never.</param>
    /// <param name="expiresAt">When it stops being current, or null if never.</param>
    /// <returns>The <see cref="ObserverLagSample"/>.</returns>
    public static ObserverLagSample Unmeasurable(ObserverLagCause cause, DateTimeOffset? sampledAt = null, DateTimeOffset? expiresAt = null) =>
        new(ObserverLagStatus.Unmeasurable, cause, null, null, null, null, null, sampledAt, expiresAt);

    /// <summary>
    /// Gets what this measurement says about the observer at a given time. A measurement past its expiry says nothing.
    /// </summary>
    /// <param name="now">The time to read it at.</param>
    /// <returns>This measurement while it is current; otherwise an unmeasurable one that keeps when this one was taken, so its age is known.</returns>
    public ObserverLagSample At(DateTimeOffset now) =>
        Status != ObserverLagStatus.Unmeasurable && (ExpiresAt is null || now > ExpiresAt)
            ? Unmeasurable(ObserverLagCause.NoSample, SampledAt, ExpiresAt)
            : this;
}
