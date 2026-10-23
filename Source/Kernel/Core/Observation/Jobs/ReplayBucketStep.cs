// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Records durable progress for one bucket without tracking every partition.
/// </summary>
/// <param name="Id">The step identifier.</param>
/// <param name="Bucket">The immutable bucket assignment.</param>
/// <param name="LastHandledEventSequenceNumber">The durable checkpoint, or unavailable before any progress.</param>
/// <param name="Completed">Whether the entire bucket was scanned, including an empty bucket.</param>
public record ReplayBucketStep(JobStepId Id, ReplayBucket Bucket, EventSequenceNumber LastHandledEventSequenceNumber, bool Completed = false);
