// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The first undelivered event and the delivery mode retained through holds, failures, retries and quarantine.
/// </summary>
/// <param name="Position">The exact position from which recovery resumes.</param>
/// <param name="Mode">The mode in which recovery resumes.</param>
/// <param name="ReplayBoundary">The inclusive replay boundary, or null for live recovery. Past this boundary recovery is live.</param>
public record FailedPartitionRecoveryCursor(
    EventSequenceNumber Position,
    FailedPartitionRecoveryMode Mode,
    EventSequenceNumber? ReplayBoundary);
