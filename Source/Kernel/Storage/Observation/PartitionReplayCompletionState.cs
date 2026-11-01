// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A partition replay completion awaiting durable settlement and successor admission.
/// </summary>
/// <param name="Partition">The replayed partition.</param>
/// <param name="LastHandled">The last event handled by the replay.</param>
/// <param name="Boundary">The scanned replay boundary.</param>
/// <param name="CoveredEpisode">The failure episode covered by this replay, if any.</param>
/// <param name="Recorded">When the completion was received.</param>
public record PartitionReplayCompletionState(Key Partition, EventSequenceNumber LastHandled, EventSequenceNumber Boundary, FailedPartitionId? CoveredEpisode, DateTimeOffset Recorded);
