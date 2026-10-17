// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The first undelivered event and the delivery mode retained through holds, failures, retries and quarantine.
/// </summary>
public record FailedPartitionRecoveryCursor
{
    /// <summary>
    /// Initializes a cursor whose boundary agrees with its delivery mode.
    /// </summary>
    /// <param name="position">The exact position from which recovery resumes.</param>
    /// <param name="mode">The mode in which recovery resumes.</param>
    /// <param name="replayBoundary">The inclusive replay boundary, required only for replay.</param>
    /// <exception cref="InvalidFailedPartitionRecoveryCursor">The mode and boundary disagree.</exception>
    public FailedPartitionRecoveryCursor(EventSequenceNumber position, FailedPartitionRecoveryMode mode, EventSequenceNumber? replayBoundary)
    {
        if (mode is not (FailedPartitionRecoveryMode.Live or FailedPartitionRecoveryMode.Replay) ||
            mode == FailedPartitionRecoveryMode.Replay != (replayBoundary is not null))
        {
            throw new InvalidFailedPartitionRecoveryCursor(mode, replayBoundary);
        }

        Position = position;
        Mode = mode;
        ReplayBoundary = replayBoundary;
    }

    /// <summary>
    /// Gets the exact position from which recovery resumes.
    /// </summary>
    public EventSequenceNumber Position { get; }

    /// <summary>
    /// Gets the recovery delivery mode.
    /// </summary>
    public FailedPartitionRecoveryMode Mode { get; }

    /// <summary>
    /// Gets the inclusive replay boundary, or null for live recovery. Past this boundary recovery is live.
    /// </summary>
    public EventSequenceNumber? ReplayBoundary { get; }
}
