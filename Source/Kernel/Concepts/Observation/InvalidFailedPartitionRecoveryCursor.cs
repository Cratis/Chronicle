// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The exception that is thrown when a recovery cursor's mode and replay boundary disagree.
/// </summary>
/// <param name="mode">The invalid recovery mode.</param>
/// <param name="replayBoundary">The supplied replay boundary.</param>
public class InvalidFailedPartitionRecoveryCursor(FailedPartitionRecoveryMode mode, EventSequenceNumber? replayBoundary)
    : Exception($"Recovery mode '{mode}' requires {(mode == FailedPartitionRecoveryMode.Replay ? "a replay boundary" : "no replay boundary")}, but received '{replayBoundary}'.");
