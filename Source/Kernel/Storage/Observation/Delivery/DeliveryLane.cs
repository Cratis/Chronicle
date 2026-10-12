// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents persisted progress for one observer delivery lane.
/// </summary>
/// <param name="U">The lane's scanned position.</param>
/// <param name="W">The lane's committed position.</param>
/// <param name="NextBatchEnd">The exclusive end of the next batch.</param>
/// <param name="Attempts">The lane attempt counter.</param>
public sealed record DeliveryLane(EventSequenceNumber U, EventSequenceNumber W, EventSequenceNumber NextBatchEnd, ulong Attempts);
