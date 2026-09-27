// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Records which partition a full replay step covers and the last event it handled successfully.
/// </summary>
/// <param name="Id">The step identifier.</param>
/// <param name="Partition">The partition being replayed.</param>
/// <param name="LastHandledEventSequenceNumber">The successful step result, or unavailable if not completed successfully.</param>
public record ReplayPartitionStep(JobStepId Id, Key Partition, EventSequenceNumber LastHandledEventSequenceNumber);
