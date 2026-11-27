// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>Describes the coverage of one requested partition repair.</summary>
/// <param name="Through">The inclusive end of the replay.</param>
/// <param name="RetainOtherCounts">Whether counts for unselected event types are retained.</param>
/// <param name="EventTypes">The ordered event types and generations to read.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PartitionRepairCoverage")]
public record PartitionRepairCoverage([Id(0)] EventSequenceNumber Through, [Id(1)] bool RetainOtherCounts, [Id(2)] EventType[] EventTypes);
