// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents shadow ownership of a partition, never used for delivery decisions.
/// </summary>
/// <param name="Partition">The claimed partition.</param>
/// <param name="Holder">The kind of work holding the claim.</param>
/// <param name="Job">The job holding the claim, when known.</param>
/// <param name="Epoch">The epoch allocated at grant.</param>
/// <param name="ResumePosition">The position from which work resumes.</param>
/// <param name="PendingWork">The pending partition work.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PartitionClaim")]
public record PartitionClaim([Id(0)] Key Partition, [Id(1)] ClaimHolderKind Holder, [Id(2)] JobId? Job, [Id(3)] ClaimEpoch Epoch, [Id(4)] EventSequenceNumber ResumePosition, [Id(5)] IReadOnlyList<PendingPartitionWork> PendingWork);
