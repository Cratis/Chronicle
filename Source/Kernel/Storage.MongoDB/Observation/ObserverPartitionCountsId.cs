// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents the composite identifier of an <see cref="ObserverPartitionCounts"/> document.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> the counts belong to.</param>
/// <param name="Partition">The string representation of the partition key the counts belong to.</param>
public record ObserverPartitionCountsId(ObserverId ObserverId, string Partition);
