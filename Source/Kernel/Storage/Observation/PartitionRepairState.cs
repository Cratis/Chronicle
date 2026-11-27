// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>Durable repair work and live-delivery hold for a partition.</summary>
/// <param name="Partition">The held partition.</param>
/// <param name="Head">The coverage owed by the next or current reader.</param>
/// <param name="HeldBackFrom">The first potentially undelivered position.</param>
/// <param name="Active">The currently authorized attempt, if any.</param>
/// <param name="Retiring">The fenced predecessor that must settle before dispatch.</param>
/// <param name="Waiting">Distinct subsequent requests in arrival order.</param>
/// <param name="CountsPrepared">Whether this head already reset its handled counts.</param>
/// <param name="CatchUpFrom">The position owed by a completed head, until catch-up has been handed off.</param>
/// <param name="AwaitingLegacyReplay">Whether an untracked legacy reader still holds the partition.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PartitionRepairState")]
public record PartitionRepairState(
    [Id(0)] Key Partition,
    [Id(1)] PartitionRepairCoverage Head,
    [Id(2)] EventSequenceNumber HeldBackFrom,
    [Id(3)] PartitionRepairGrant? Active,
    [Id(4)] PartitionRepairGrant? Retiring,
    [Id(5)] PartitionRepairCoverage[] Waiting,
    [Id(6)] bool CountsPrepared = false,
    [Id(7)] EventSequenceNumber? CatchUpFrom = null,
    [Id(8)] bool AwaitingLegacyReplay = false);
