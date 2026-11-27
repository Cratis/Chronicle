// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>Identifies an exact job that was or might have been submitted.</summary>
/// <param name="Job">The job's identity.</param>
/// <param name="Epoch">The persisted partition claim epoch.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PartitionRepairGrant")]
public record PartitionRepairGrant([Id(0)] JobId Job, [Id(1)] ClaimEpoch Epoch);
