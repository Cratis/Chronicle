// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A step's durable checkpoint or outcome. Its row carries the durable position, keyed by job and partition.
/// </summary>
/// <param name="JobId">The execution's job id.</param>
/// <param name="Partition">The partition.</param>
/// <param name="Outcome">Checkpoint, success, stop, failure or hold.</param>
/// <param name="Recorded">When the checkpoint or outcome became durable.</param>
/// <param name="RecoveryOwner">The owner required before a recovery record may advance a failed entry.</param>
public record ObserverProgressRecord(JobId JobId, Key Partition, ObserverWorkOutcome Outcome, DateTimeOffset Recorded, FailedPartitionRecoveryOwner? RecoveryOwner = null);
